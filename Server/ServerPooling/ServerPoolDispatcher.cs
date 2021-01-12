using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.ServerPooling
{
    internal class ServerPoolDispatcher
    {
        private readonly Dictionary<string, IRequestHandler> _handlers = new Dictionary<string, IRequestHandler>();
        private readonly Dictionary<string, CancellationTokenSource> _cancellationSources = new Dictionary<string, CancellationTokenSource>();
        private readonly ISiLAServer _server;
        private readonly ILoggingChannel _loggingChannel;

        public ServerPoolDispatcher(ILoggingChannelFactory loggingChannelFactory)
        {
            _loggingChannel = loggingChannelFactory.CreateChannel( typeof( ServerPoolDispatcher ) );
        }

        public void RegisterHandler(string fullyQualifiedId, IRequestHandler handler)
        {
            _handlers.Add( fullyQualifiedId, handler );
        }

        internal void HandleClientRequest( SilaClientMessage message, IClientStreamWriter<SilaServerMessage> responseStream )
        {
            switch(message.Type)
            {
                case SilaClientMessageType.CommandExecution:
                    HandleClientRequestCore( message.CommandExecution.FullyQualifiedCommandId, message, responseStream );
                    break;
                case SilaClientMessageType.CommandInitiation:
                    HandleClientRequestCore( message.CommandInitiation.FullyQualifiedCommandId, message, responseStream );
                    break;
                case SilaClientMessageType.CommandExecutionInfoSubscription:
                    var execution = _server.GetCommandExecution( message.CommandExecutionInfoSubscription.ExecutionUUID.CommandId );
                    GetCommandHandler( execution ).HandleExecutionStateSubscription( message.RequestUuid, execution, responseStream, this );
                    break;
                case SilaClientMessageType.CommandIntermediateResponseSubscription:
                    var execution2 = _server.GetCommandExecution( message.CommandIntermediateResponseSubscription.ExecutionUUID.CommandId );
                    GetCommandHandler( execution2 ).HandleIntermediatesSubscription( message.RequestUuid, execution2, responseStream, this );
                    break;
                case SilaClientMessageType.CommandGetResponse:
                    var execution3 = _server.GetCommandExecution( message.CommandExecutionInfoSubscription.ExecutionUUID.CommandId );
                    GetCommandHandler( execution3 ).HandleResultRequest( message.RequestUuid, execution3, responseStream, this );
                    break;
                case SilaClientMessageType.MetadataRequest:
                    HandleClientRequestCore( message.MetadataRequest.FullyQualifiedMetadataId, message, responseStream );
                    break;
                case SilaClientMessageType.PropertyRead:
                    HandleClientRequestCore( message.PropertyRead.FullyQualifiedPropertyId, message, responseStream );
                    break;
                case SilaClientMessageType.PropertySubscription:
                    HandleClientRequestCore( message.PropertySubscription.FullyQualifiedPropertyId, message, responseStream );
                    break;
                case SilaClientMessageType.CancelCommandExecutionInfoSubscription:
                case SilaClientMessageType.CancelCommandIntermediateResponseSubscription:
                case SilaClientMessageType.CancelPropertySubscription:
                    TryCancel( message.RequestUuid );
                    break;
                case SilaClientMessageType.CreateBinaryRequest:
                    break;
                case SilaClientMessageType.UploadChunkRequest:
                    break;
                case SilaClientMessageType.DeleteBinaryRequest:
                    break;
                case SilaClientMessageType.GetBinaryInfoRequest:
                    break;
                case SilaClientMessageType.GetChunkRequest:
                    break;
            }
        }

        private ICommandRequestHandler GetCommandHandler(ObservableCommandExecution command)
        {
            if (_handlers.TryGetValue(command.CommandIdentifier, out var handler) && handler is ICommandRequestHandler commandHandler)
            {
                return commandHandler;
            }
            return null;
        }

        private void HandleClientRequestCore( string id, SilaClientMessage message, IClientStreamWriter<SilaServerMessage> responseStream )
        {
            if (_handlers.TryGetValue(id, out var handler))
            {
                handler.HandleRequest( message, responseStream, this );
            }
            else
            {
                
            }
        }

        private void TryCancel(string id)
        {
            lock(_cancellationSources)
            {
                if(_cancellationSources.TryGetValue( id, out var source ))
                {
                    source.Cancel();
                    _cancellationSources.Remove( id );
                    source.Dispose();
                }
            }
        }

        public CancellationToken CreateCancellationToken(string id)
        {
            lock(_cancellationSources)
            {
                if(!_cancellationSources.TryGetValue( id, out var source ))
                {
                    source = new CancellationTokenSource();
                    _cancellationSources.Add( id, source );
                }
                return source.Token;
            }
        }

        public void VoidCancellationToken(string id)
        {
            lock(_cancellationSources)
            {
                if(_cancellationSources.ContainsKey( id ))
                {
                    _cancellationSources.Remove( id );
                }
            }
        }
    }
}
