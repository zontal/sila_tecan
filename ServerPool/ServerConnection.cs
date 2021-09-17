using Grpc.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.ServerPooling
{
    internal class ServerConnection : IClientChannel, IBinaryStoreChannel
    {
        private readonly IAsyncStreamReader<SilaServerMessage> _responseStream;
        private readonly IServerStreamWriter<SilaClientMessage> _requestStream;
        private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private readonly CancellationToken _originalCancellationToken;
        private readonly ILoggingChannel _loggingChannel;
        private readonly ConcurrentDictionary<string, IResponseHandler> _responseHandlers = new ConcurrentDictionary<string, IResponseHandler>();

        public ServerConnection( ILoggingChannelFactory loggingChannelFactory, IServerStreamWriter<SilaClientMessage> requestStream, IAsyncStreamReader<SilaServerMessage> responseStream, CancellationToken cancellationToken )
        {
            _loggingChannel = loggingChannelFactory.CreateChannel( typeof( ServerConnection ) );
            _requestStream = requestStream;
            _responseStream = responseStream;
            cancellationToken.Register( _cancellationToken.Cancel );
            cancellationToken.Register( CancelAll );
            _originalCancellationToken = cancellationToken;
        }

        public void CancelAll()
        {
            foreach(var handler in _responseHandlers)
            {
                handler.Value.Cancel();
            }
        }

        public CancellationToken CancellationToken => _cancellationToken.Token;

        public async Task Run()
        {
            while (await _responseStream.MoveNext(CancellationToken))
            {
                var message = _responseStream.Current;
                if (_responseHandlers.TryGetValue(message.RequestUuid, out var handler))
                {
                    try
                    {
                        if(!handler.ProcessMessage( message ))
                        {
                            _loggingChannel.Log( $"Server message of type {message.Type} came unexpected for request id {message.RequestUuid}.".ToString, Severity.Warning );
                        }
                    }
                    catch (Exception exception)
                    {
                        _loggingChannel.LogException( $"Error processing message of type {message.Type}".ToString, exception );
                    }
                }
            }
        }

        public Client.ChannelState State => _cancellationToken.IsCancellationRequested ? Client.ChannelState.Shutdown : Client.ChannelState.Ready;

        /// <inheritdoc />
        public T ReadProperty<T>( string serviceName, string propertyName, string propertyIdentifier, IClientCallInfo callInfo ) where T : class, ISilaTransferObject
        {
            return ReadProperty( serviceName, propertyName, propertyIdentifier, callInfo, ByteSerializer.GetDefault<T>() );
        }

        /// <inheritdoc />
        public T ReadProperty<T>( string serviceName, string propertyName, string propertyIdentifier, IClientCallInfo callInfo, ByteSerializer<T> serializer ) where T : class, ISilaTransferObject
        {
            return ReadPropertyAsync( serviceName, propertyName, propertyIdentifier, callInfo, serializer ).Result;
        }

        /// <inheritdoc />
        public Task<T> ReadPropertyAsync<T>( string serviceName, string propertyName, string propertyIdentifier, IClientCallInfo callInfo, ByteSerializer<T> serializer ) where T : class, ISilaTransferObject
        {
            var requestId = Guid.NewGuid().ToString();
            var handler = new UnobservablePropertyHandler<T>( requestId, serializer.Deserializer );
            return SendAndGetResponse( new SilaClientMessage
            {
                RequestUuid = requestId,
                PropertyRead = SetMetadata( new PropertyRead
                {
                    FullyQualifiedPropertyId = propertyIdentifier,
                }, callInfo )
            }, handler );
        }

        /// <inheritdoc />
        public Task SubscribeProperty<T>( string serviceName, string propertyName, string propertyIdentifier, Action<T> newValueReceived, IClientCallInfo callInfo, CancellationToken cancellationToken = default ) where T : class, ISilaTransferObject
        {
            return SubscribeProperty( serviceName, propertyName, propertyIdentifier, newValueReceived, callInfo, ByteSerializer.GetDefault<T>(), cancellationToken );
        }

        /// <inheritdoc />
        public Task SubscribeProperty<T>( string serviceName, string propertyName, string propertyIdentifier, Action<T> newValueReceived, IClientCallInfo callInfo, ByteSerializer<T> serializer, CancellationToken cancellationToken = default ) where T : class, ISilaTransferObject
        {
            var requestId = Guid.NewGuid().ToString();
            var handler = new PropertySubscriptionHandler<T>( serializer.Deserializer, newValueReceived );
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register( () =>
                 {
                     _requestStream.WriteAsync( new SilaClientMessage
                     {
                         RequestUuid = requestId,
                         CancelPropertySubscription = new EmptyMessage()
                     } );
                     handler.Complete();
                 } );
            }
            var subscriptionTask = SendAndGetResponse( new SilaClientMessage
            {
                RequestUuid = requestId,
                PropertySubscription = SetMetadata( new PropertyRead
                {
                    FullyQualifiedPropertyId = propertyIdentifier,
                }, callInfo )
            }, handler );
            handler.WaitForFirstResponse( cancellationToken );
            return subscriptionTask;
        }

        /// <inheritdoc />
        public void ExecuteUnobservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
        {
            ExecuteUnobservableCommand( serviceName, commandName, request, callInfo, ByteSerializer.GetDefault<TRequest>() );
        }

        /// <inheritdoc />
        public void ExecuteUnobservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject
        {
            ExecuteUnobservableCommandAsync( serviceName, commandName, request, callInfo, requestSerializer ).Wait();
        }

        /// <inheritdoc />
        public Task ExecuteUnobservableCommandAsync<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject
        {
            var requestId = Guid.NewGuid().ToString();
            var handler = new UnobservableCommandHandler();
            return SendAndGetResponse( new SilaClientMessage
            {
                RequestUuid = requestId,
                CommandExecution = new CommandExecution
                {
                    FullyQualifiedCommandId = request.CommandIdentifier,
                    CommandParameter = SetMetadata( new CommandParameter
                    {
                        Parameters = requestSerializer.Serializer( request ),
                    }, callInfo )
                }
            }, handler );
        }

        /// <inheritdoc />
        public TResponse ExecuteUnobservableCommand<TRequest, TResponse>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
            where TResponse : class
        {
            return ExecuteUnobservableCommand( serviceName, commandName, request, callInfo, ByteSerializer.GetDefault<TRequest>(), ByteSerializer.GetDefault<TResponse>() );
        }

        /// <inheritdoc />
        public TResponse ExecuteUnobservableCommand<TRequest, TResponse>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer,
            ByteSerializer<TResponse> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponse : class
        {
            var responseTask = ExecuteUnobservableCommandAsync( serviceName, commandName, request, callInfo, requestSerializer, responseSerializer );
            return responseTask.Result;
        }

        /// <inheritdoc />
        public Task<TResponse> ExecuteUnobservableCommandAsync<TRequest, TResponse>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer,
            ByteSerializer<TResponse> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponse : class
        {
            var requestId = Guid.NewGuid().ToString();
            var handler = new UnobservableCommandHandler<TResponse>( responseSerializer.Deserializer );
            return SendAndGetResponse( new SilaClientMessage
            {
                RequestUuid = requestId,
                CommandExecution = new CommandExecution
                {
                    FullyQualifiedCommandId = request.CommandIdentifier,
                    CommandParameter = SetMetadata( new CommandParameter
                    {
                        Parameters = requestSerializer.Serializer( request ),
                    }, callInfo )
                }
            }, handler );
        }

        /// <inheritdoc />
        public IObservableCommand ExecuteObservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
        {
            return ExecuteObservableCommand( serviceName, commandName, request, errorConverter, callInfo, ByteSerializer.GetDefault<TRequest>() );
        }

        /// <inheritdoc />
        public IObservableCommand ExecuteObservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject
        {
            var requestId = Guid.NewGuid().ToString();
            var handler = new ObservableCommandHandler( requestId, _requestStream, errorConverter, () => _responseHandlers.TryRemove( requestId, out _ ) );
            _responseHandlers.TryAdd( requestId, handler );
            handler.Start();
            _ = _requestStream.WriteAsync( new SilaClientMessage
            {
                RequestUuid = requestId,
                CommandInitiation = new CommandInitiation
                {
                    FullyQualifiedCommandId = request.CommandIdentifier,
                    CommandParameter = SetMetadata( new CommandParameter
                    {
                        Parameters = requestSerializer.Serializer( request ),
                    }, callInfo )
                }
            } );
            return handler;
        }

        /// <inheritdoc />
        public IObservableCommand<TResponse> ExecuteObservableCommand<TRequest, TResponse, TResponseDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TResponseDto, TResponse> responseConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
            where TResponseDto : class, ISilaTransferObject
        {
            return ExecuteObservableCommand( serviceName, commandName, request, responseConverter, errorConverter, callInfo, ByteSerializer.GetDefault<TRequest>(), ByteSerializer.GetDefault<TResponseDto>() );
        }

        /// <inheritdoc />
        public IObservableCommand<TResponse> ExecuteObservableCommand<TRequest, TResponse, TResponseDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TResponseDto, TResponse> responseConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer,
            ByteSerializer<TResponseDto> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponseDto : class, ISilaTransferObject
        {
            var requestId = Guid.NewGuid().ToString();
            var handler = new ObservableCommandHandler<TResponse>( requestId, _requestStream, errorConverter, data => responseConverter( responseSerializer.Deserializer(data) ), () => _responseHandlers.TryRemove( requestId, out _ ) );
            _responseHandlers.TryAdd( requestId, handler );
            handler.Start();
            _ = _requestStream.WriteAsync( new SilaClientMessage
            {
                RequestUuid = requestId,
                CommandInitiation = new CommandInitiation
                {
                    FullyQualifiedCommandId = request.CommandIdentifier,
                    CommandParameter = SetMetadata( new CommandParameter
                    {
                        Parameters = requestSerializer.Serializer( request ),
                    }, callInfo )
                }
            } );
            return handler;
        }

        /// <inheritdoc />
        public IIntermediateObservableCommand<TIntermediate> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TIntermediateDto, TIntermediate> intermediateConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class, ISilaTransferObject
        {
            return ExecuteIntermediatesCommand( serviceName, commandName, request, intermediateConverter, errorConverter, callInfo, ByteSerializer.GetDefault<TRequest>(), ByteSerializer.GetDefault<TIntermediateDto>() );
        }

        /// <inheritdoc />
        public IIntermediateObservableCommand<TIntermediate> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TIntermediateDto, TIntermediate> intermediateConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer,
            ByteSerializer<TIntermediateDto> intermediateSerializer )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class, ISilaTransferObject
        {
            var requestId = Guid.NewGuid().ToString();
            var handler = new IntermediateCommandHandler<TIntermediate>( requestId, _requestStream, errorConverter, data => intermediateConverter(intermediateSerializer.Deserializer(data)), () => _responseHandlers.TryRemove( requestId, out _ ) );
            _responseHandlers.TryAdd( requestId, handler );
            handler.Start();
            _ = _requestStream.WriteAsync( new SilaClientMessage
            {
                RequestUuid = requestId,
                CommandInitiation = new CommandInitiation
                {
                    FullyQualifiedCommandId = request.CommandIdentifier,
                    CommandParameter = SetMetadata( new CommandParameter
                    {
                        Parameters = requestSerializer.Serializer( request ),
                    }, callInfo )
                }
            } );
            return handler;
        }

        /// <inheritdoc />
        public IIntermediateObservableCommand<TIntermediate, TResponse> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto, TResponse, TResponseDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TIntermediateDto, TIntermediate> intermediateConverter,
            Func<TResponseDto, TResponse> responseConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class, ISilaTransferObject
            where TResponseDto : class, ISilaTransferObject
        {
            return ExecuteIntermediatesCommand(
                serviceName,
                commandName,
                request,
                intermediateConverter,
                responseConverter,
                errorConverter,
                callInfo,
                ByteSerializer.GetDefault<TRequest>(),
                ByteSerializer.GetDefault<TIntermediateDto>(),
                ByteSerializer.GetDefault<TResponseDto>() );
        }

        /// <inheritdoc />
        public IIntermediateObservableCommand<TIntermediate, TResponse> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto, TResponse, TResponseDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TIntermediateDto, TIntermediate> intermediateConverter,
            Func<TResponseDto, TResponse> responseConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer,
            ByteSerializer<TIntermediateDto> intermediateSerializer,
            ByteSerializer<TResponseDto> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class, ISilaTransferObject
            where TResponseDto : class, ISilaTransferObject
        {
            var requestId = Guid.NewGuid().ToString();
            var handler = new IntermediateCommandHandler<TIntermediate, TResponse>( 
                requestId, 
                _requestStream, 
                errorConverter,
                data => intermediateConverter( intermediateSerializer.Deserializer( data ) ), 
                data => responseConverter( responseSerializer.Deserializer( data ) ),
                () => _responseHandlers.TryRemove( requestId, out _ ) );
            _responseHandlers.TryAdd( requestId, handler );
            handler.Start();
            _ = _requestStream.WriteAsync( new SilaClientMessage
            {
                RequestUuid = requestId,
                CommandInitiation = new CommandInitiation
                {
                    FullyQualifiedCommandId = request.CommandIdentifier,
                    CommandParameter = SetMetadata( new CommandParameter
                    {
                        Parameters = requestSerializer.Serializer( request ),
                    }, callInfo )
                }
            } );
            return handler;
        }

        private static CommandParameter SetMetadata(CommandParameter parameter, IClientCallInfo callInfo)
        {
            CopyMetadata(callInfo, parameter.Metadata);
            return parameter;
        }

        private static PropertyRead SetMetadata( PropertyRead parameter, IClientCallInfo callInfo )
        {
            CopyMetadata( callInfo, parameter.Metadatas );
            return parameter;
        }

        private static CreateBinaryUploadRequest SetMetadata( CreateBinaryUploadRequest parameter, IClientCallInfo callInfo )
        {
            CopyMetadata( callInfo, parameter.Metadata );
            return parameter;
        }

        private static void CopyMetadata(IClientCallInfo source, List<Metadata> target)
        {
            if(source?.Metadata != null)
            {
                foreach(var metadata in source.Metadata)
                {
                    target.Add( new Metadata
                    {
                        FullyQualifiedMetadataId = metadata.Key,
                        Value = metadata.Value
                    } );
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAffected( string serviceName, string metadataName, string metadataIdentifier )
        {
            var requestId = Guid.NewGuid().ToString();
            var handler = new MetadataHandler();
            return SendAndGetResponse( new SilaClientMessage
            {
                RequestUuid = requestId,
                MetadataRequest = new GetFCPAffectedByMetadataRequest()
                {
                    FullyQualifiedMetadataId = metadataIdentifier
                }
            }, handler ).Result;
        }

        /// <inheritdoc />
        public IBinaryStore CreateBinaryStore( string commandParameterIdentifier, IClientExecutionManager executionManager )
        {
            return new PoolBinaryStore( commandParameterIdentifier, this, executionManager );
        }

        /// <inheritdoc />
        public Exception ConvertException( Exception ex,
            Func<string, string, Exception> executionErrorConversion = null )
        {
            return ErrorHandling.ConvertException( ex, executionErrorConversion );
        }

        public Task UploadChunk( UploadChunkRequestDto uploadChunkRequestDto )
        {
            var requestId = Guid.NewGuid().ToString();
            return _requestStream.WriteAsync( new SilaClientMessage
            {
                RequestUuid = requestId,
                UploadChunkRequest = uploadChunkRequestDto
            } );
        }

        public Task<CreateBinaryResponseDto> CreateBinaryAsync( CreateBinaryRequestDto createBinaryRequestDto, IClientCallInfo callInfo )
        {
            var requestId = Guid.NewGuid().ToString();
            var handler = new CreateBinaryHandler();
            return SendAndGetResponse( new SilaClientMessage
            {
                RequestUuid = requestId,
                CreateBinaryRequest = SetMetadata( new CreateBinaryUploadRequest
                {
                    CreateBinaryRequest = createBinaryRequestDto
                }, callInfo )
            }, handler ); ;
        }

        public Task<GetChunkResponseDto> GetChunk( GetChunkRequestDto getChunkRequestDto )
        {
            var requestId = Guid.NewGuid().ToString();
            var handler = new GetChunkHandler(getChunkRequestDto.BinaryTransferUUID);
            return SendAndGetResponse( new SilaClientMessage
            {
                RequestUuid = requestId,
                GetChunkRequest = getChunkRequestDto
            }, handler );
        }

        public Task<GetBinaryInfoResponseDto> GetBinaryInfoAsync( GetBinaryInfoRequestDto getBinaryInfoRequestDto )
        {
            var requestId = Guid.NewGuid().ToString();
            var handler = new GetBinaryInfoHandler(getBinaryInfoRequestDto.BinaryTransferUUID);
            return SendAndGetResponse( new SilaClientMessage
            {
                RequestUuid = requestId,
                GetBinaryInfoRequest = getBinaryInfoRequestDto
            }, handler );
        }

        private async Task<T> SendAndGetResponse<T>(SilaClientMessage message, IResponseHandler<T> handler)
        {
            try
            {
                _responseHandlers.TryAdd( message.RequestUuid, handler );
                await _requestStream.WriteAsync( message );
                return await handler.ResponseTask;
            }
            catch(AggregateException aggregate) when (aggregate.InnerException is TaskCanceledException)
            {
                throw new OperationCanceledException();
            }
            catch(TaskCanceledException)
            {
                throw new OperationCanceledException();
            }
            finally
            {
                _responseHandlers.TryRemove( message.RequestUuid, out _ );
            }
        }

        public Task DeleteBinaryDownloadAsync( DeleteBinaryRequestDto deleteBinaryRequestDto, IClientCallInfo callInfo )
        {
            var requestId = Guid.NewGuid().ToString();
            return _requestStream.WriteAsync( new SilaClientMessage
            {
                RequestUuid = requestId,
                DeleteDownloadedBinaryRequest = deleteBinaryRequestDto
            } ); ;
        }

        public void Dispose()
        {
            if (!_cancellationToken.IsCancellationRequested)
            {
                _cancellationToken.Cancel();
            }
            _cancellationToken.Dispose();
        }
    }
}
