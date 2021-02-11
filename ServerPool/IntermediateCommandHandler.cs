using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace Tecan.Sila2.ServerPooling
{
    internal class IntermediateCommandHandler<TIntermediate> : ObservableCommandHandler, IIntermediateObservableCommand<TIntermediate>
    {
        private readonly Func<byte[], TIntermediate> _intermediateResponseConverter;
        private readonly Channel<TIntermediate> _intermediateValuesChannel;
        private bool _intermediatesValuesRequested = false;

        public IntermediateCommandHandler( string requestUuid, IServerStreamWriter<SilaClientMessage> requestStream, Func<string, string, Exception> errorConverter, Func<byte[], TIntermediate> intermediateConverter, Action cleanupAction ) : base( requestUuid, requestStream, errorConverter, cleanupAction )
        {
            _intermediateResponseConverter = intermediateConverter;
            _intermediateValuesChannel = Channels.CreateIntermediateChannel<TIntermediate>();
        }

        public ChannelReader<TIntermediate> IntermediateValues => _intermediateValuesChannel.Reader;

        protected override bool HandleIntermediateResponse( ObservableCommandResponse intermediateResponse )
        {
            _intermediateValuesChannel.Writer.TryWrite( _intermediateResponseConverter( intermediateResponse.Result ) );
            return true;
        }

        protected override void TraceCommandExecution( CommandExecutionUuid executionUuid )
        {
            base.TraceCommandExecution( executionUuid );
            _intermediatesValuesRequested = true;
            SendMessage( new SilaClientMessage
            {
                CommandIntermediateResponseSubscription = new CommandExecutionReference
                {
                    ExecutionUUID = executionUuid
                }
            } );
        }

        public override void Cancel()
        {
            base.Cancel();
            if(_intermediatesValuesRequested)
            {
                SendMessage( new SilaClientMessage
                {
                    CancelCommandIntermediateResponseSubscription = new EmptyMessage()
                } );
            }
        }

        protected override void Cleanup( Exception exception )
        {
            base.Cleanup( exception );
            _intermediateValuesChannel.Writer.TryComplete( exception );
        }
    }

    internal class IntermediateCommandHandler<TIntermediate, TResponse> : ObservableCommandHandler<TResponse>, IIntermediateObservableCommand<TIntermediate, TResponse>
    {
        private readonly Func<byte[], TIntermediate> _intermediateResponseConverter;
        private readonly Channel<TIntermediate> _intermediateValuesChannel;
        private bool _intermediatesValuesRequested = false;

        public IntermediateCommandHandler( string requestUuid, IServerStreamWriter<SilaClientMessage> requestStream, Func<string, string, Exception> errorConverter, Func<byte[], TIntermediate> intermediateConverter, Func<byte[], TResponse> responseConverter, Action cleanupAction ) : base( requestUuid, requestStream, errorConverter, responseConverter, cleanupAction )
        {
            _intermediateResponseConverter = intermediateConverter;
            _intermediateValuesChannel = Channels.CreateIntermediateChannel<TIntermediate>();
        }

        public ChannelReader<TIntermediate> IntermediateValues => _intermediateValuesChannel.Reader;

        protected override bool HandleIntermediateResponse( ObservableCommandResponse intermediateResponse )
        {
            _intermediateValuesChannel.Writer.TryWrite( _intermediateResponseConverter( intermediateResponse.Result ) );
            return true;
        }

        protected override void TraceCommandExecution( CommandExecutionUuid executionUuid )
        {
            base.TraceCommandExecution( executionUuid );
            _intermediatesValuesRequested = true;
            SendMessage( new SilaClientMessage
            {
                CommandIntermediateResponseSubscription = new CommandExecutionReference
                {
                    ExecutionUUID = executionUuid
                }
            } );
        }

        public override void Cancel()
        {
            base.Cancel();
            if(_intermediatesValuesRequested)
            {
                SendMessage( new SilaClientMessage
                {
                    CancelCommandIntermediateResponseSubscription = new EmptyMessage()
                } );
            }
        }

        protected override void Cleanup( Exception exception )
        {
            base.Cleanup( exception );
            _intermediateValuesChannel.Writer.Complete( exception );
        }

    }
}
