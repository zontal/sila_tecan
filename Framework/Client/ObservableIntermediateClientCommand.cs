using System;
using System.Threading.Channels;
using Grpc.Core;

namespace Tecan.Sila2.Client
{
    internal class ObservableIntermediateClientCommand<TIntermediateResponse, TIntermediate> : ObservableClientCommand, IIntermediateObservableCommand<TIntermediate> where TIntermediateResponse : class
    {
        private readonly Channel<TIntermediate> _intermediateValuesChannel;

        public Func<TIntermediateResponse, TIntermediate> IntermediateConverter { get; }

        public Method<CommandExecutionUuid, TIntermediateResponse> IntermediateMethod { get; }

        public ChannelReader<TIntermediate> IntermediateValues => _intermediateValuesChannel.Reader;

        public ObservableIntermediateClientCommand(
            IClientCallInfo executionManager,
            CallInvoker callInvoker,
            CommandExecutionUuid commandId,
            Method<CommandExecutionUuid, ExecutionState> stateMethod,
            Method<CommandExecutionUuid, TIntermediateResponse> intermediateMethod,
            Func<TIntermediateResponse, TIntermediate> intermediateConverter,
            Func<DefinedExecutionErrorDto, Exception> errorConverter)
            : base(executionManager, callInvoker, commandId, stateMethod, errorConverter)
        {
            IntermediateConverter = intermediateConverter;
            IntermediateMethod = intermediateMethod;

            _intermediateValuesChannel = Channels.CreateIntermediateChannel<TIntermediate>();
        }

        public override void Start()
        {
            base.Start();
            FetchIntermediateResponses();
        }

        public override void Cleanup( Exception exception )
        {
            Cancel();
            base.Cleanup( exception );
        }

        private async void FetchIntermediateResponses()
        {
            Exception exception = null;
            try
            {
                using( var streamingCall =
                    CallInvoker.AsyncServerStreamingCall( IntermediateMethod, null, CallInfo.Options.WithCancellationToken( CancellationToken ), CommandId ) )
                {
                    var responseStream = streamingCall.ResponseStream;
                    while( await responseStream.MoveNext(default) )
                    {
                        var intermediateResult = IntermediateConverter( responseStream.Current );
                        _intermediateValuesChannel.Writer.TryWrite( intermediateResult );
                    }
                }
            }
            catch( RpcException ex )
            {
                // if the remote tells us to go away, there is no further intermediate result
                if( !ex.Status.Detail.StartsWith( "GOAWAY" ) )
                {
                    throw;
                }
            }
            catch( Exception ex )
            {
                exception = ex;
                throw;
            }
            finally
            {
                _intermediateValuesChannel.Writer.Complete( exception );
            }
        }
    }
    
    internal class ObservableIntermediateClientCommand<TIntermediateResponse, TIntermediate, TResultResponse, TResult> : ObservableClientCommand<TResultResponse, TResult>, IIntermediateObservableCommand<TIntermediate, TResult>
        where TIntermediateResponse : class
        where TResultResponse : class
    {
        private readonly Channel<TIntermediate> _intermediateValuesChannel;

        public Func<TIntermediateResponse, TIntermediate> IntermediateConverter { get; }

        public Method<CommandExecutionUuid, TIntermediateResponse> IntermediateMethod { get; }

        public ChannelReader<TIntermediate> IntermediateValues => _intermediateValuesChannel.Reader;

        public ObservableIntermediateClientCommand(
            IClientCallInfo callInfo,
            CallInvoker callInvoker,
            CommandExecutionUuid commandId,
            Method<CommandExecutionUuid, ExecutionState> stateMethod,
            Method<CommandExecutionUuid, TIntermediateResponse> intermediateMethod,
            Method<CommandExecutionUuid, TResultResponse> resultMethod,
            Func<TIntermediateResponse, TIntermediate> intermediateConverter,
            Func<TResultResponse, TResult> resultConverter,
            Func<DefinedExecutionErrorDto, Exception> errorConverter )
            : base( callInfo, callInvoker, commandId, stateMethod, resultMethod, resultConverter, errorConverter )
        {
            IntermediateConverter = intermediateConverter;
            IntermediateMethod = intermediateMethod;

            _intermediateValuesChannel = Channels.CreateIntermediateChannel<TIntermediate>();
        }

        public override void Start()
        {
            base.Start();
            FetchIntermediateResponses();
        }

        public override void Cleanup( Exception exception )
        {
            Cancel();
            base.Cleanup( exception );
        }

        private async void FetchIntermediateResponses()
        {
            Exception exception = null;
            try
            {
                using(var streamingCall =
                    CallInvoker.AsyncServerStreamingCall( IntermediateMethod, null, CallInfo.Options.WithCancellationToken( CancellationToken ), CommandId ))
                {
                    var responseStream = streamingCall.ResponseStream;
                    while(await responseStream.MoveNext(default))
                    {
                        var intermediateResult = IntermediateConverter( responseStream.Current );
                        _intermediateValuesChannel.Writer.TryWrite( intermediateResult );
                    }
                }
            }
            catch(RpcException ex)
            {
                // if the remote tells us to go away, there is no further intermediate result
                if(!ex.Status.Detail.StartsWith( "GOAWAY" ))
                {
                    throw;
                }
            }
            catch(Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                _intermediateValuesChannel.Writer.Complete( exception );
            }
        }
    }
}