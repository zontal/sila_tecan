using Common.Logging;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Discovery
{
    internal class SilaChannel : IClientChannel
    {
        private readonly GrpcChannel _channel;
        private static readonly ILog _log = LogManager.GetLogger(typeof(SilaChannel));

        public Client.ChannelState State => ChannelState.Idle;

        public CallInvoker CallInvoker { get; }

        public bool IsServerInitiated => false;

        public SilaChannel( GrpcChannel channel )
        {
            _channel = channel;
            CallInvoker = channel.CreateCallInvoker();
        }

        /// <inheritdoc />
        public T ReadProperty<T>( string serviceName, string propertyName, string propertyIdentifier, IClientCallInfo callInfo ) where T : class, ISilaTransferObject
        {
            return ReadProperty( serviceName, propertyName, propertyIdentifier, callInfo, ProtobufMarshaller<T>.Default );
        }

        /// <inheritdoc />
        public T ReadProperty<T>( string serviceName, string propertyName, string propertyIdentifier, IClientCallInfo callInfo, ByteSerializer<T> serializer ) where T : class, ISilaTransferObject
        {
            return ReadProperty( serviceName, propertyName, propertyIdentifier, callInfo, ToMarshaller(serializer) );
        }

        private T ReadProperty<T>( string serviceName, string propertyName, string propertyIdentifier, IClientCallInfo callInfo, Marshaller<T> serializer ) where T : class, ISilaTransferObject
        {
            var propertyMethod = new Method<EmptyRequest, T>( MethodType.Unary, serviceName, "Get_" + propertyName, EmptyRequestMarshaller.Instance, serializer );
            return CallInvoker.BlockingUnaryCall( propertyMethod, null, callInfo.ToCallOptions(), EmptyRequest.Instance );
        }

        /// <inheritdoc />
        public Task<T> ReadPropertyAsync<T>( string serviceName, string propertyName, string propertyIdentifier, IClientCallInfo callInfo, ByteSerializer<T> serializer ) where T : class, ISilaTransferObject
        {
            var propertyMethod = new Method<EmptyRequest, T>( MethodType.Unary, serviceName, "Get_" + propertyName, EmptyRequestMarshaller.Instance, ToMarshaller(serializer) );
            return CallInvoker.AsyncUnaryCall( propertyMethod, null, callInfo.ToCallOptions(), EmptyRequest.Instance ).ResponseAsync;
        }

        /// <inheritdoc />
        public Task SubscribeProperty<T>( string serviceName, string propertyName, string propertyIdentifier, Action<T> newValueReceived, IClientCallInfo callInfo, CancellationToken cancellationToken = default ) where T : class, ISilaTransferObject
        {
            return SubscribeProperty( serviceName, propertyName, newValueReceived, callInfo, ProtobufMarshaller<T>.Default, null, cancellationToken );
        }

        /// <inheritdoc />
        public Task SubscribeProperty<T>( string serviceName, string propertyName, string propertyIdentifier, Action<T> newValueReceived, IClientCallInfo callInfo, ByteSerializer<T> serializer, CancellationToken cancellationToken = default ) where T : class, ISilaTransferObject
        {
            return SubscribeProperty( serviceName, propertyName, newValueReceived, callInfo, ToMarshaller( serializer ), null, cancellationToken );
        }

        /// <inheritdoc />
        public Task SubscribeProperty<T>(string serviceName, string propertyName, string propertyIdentifier, Action<T> newValueReceived, IClientCallInfo callInfo, Action cleanup, CancellationToken cancellationToken = default) where T : class, ISilaTransferObject
        {
            return SubscribeProperty(serviceName, propertyName, newValueReceived, callInfo, ProtobufMarshaller<T>.Default, cleanup, cancellationToken);
        }

        /// <inheritdoc />
        public Task SubscribeProperty<T>(string serviceName, string propertyName, string propertyIdentifier, Action<T> newValueReceived, IClientCallInfo callInfo, ByteSerializer<T> serializer, Action cleanup, CancellationToken cancellationToken = default) where T : class, ISilaTransferObject
        {
            return SubscribeProperty(serviceName, propertyName, newValueReceived, callInfo, ToMarshaller(serializer), cleanup, cancellationToken);
        }

        private Task SubscribeProperty<T>( string serviceName, string propertyName, Action<T> newValueReceived, IClientCallInfo callInfo, Marshaller<T> serializer,
            Action cleanup, CancellationToken cancellationToken ) where T : class, ISilaTransferObject
        {
            var subscribeMethod = new Method<EmptyRequest, T>( MethodType.ServerStreaming, serviceName, "Subscribe_" + propertyName, EmptyRequestMarshaller.Instance, serializer );

            var asyncCall = CallInvoker.AsyncServerStreamingCall( subscribeMethod, null,
                callInfo.ToCallOptions().WithCancellationToken( cancellationToken ), EmptyRequest.Instance );
            var responseStream = asyncCall.ResponseStream;

            var firstResponse = responseStream.MoveNext( cancellationToken );

            var retrievedFirstResult = false;
            try
            {
                retrievedFirstResult = firstResponse.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception exc)
            {
                throw ClientErrorHandling.ConvertException(exc);
            }

            if (retrievedFirstResult)
            {
                newValueReceived(responseStream.Current);
                return ContinueSubscription(newValueReceived, responseStream, cancellationToken).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _log.Error($"Subscription of property {propertyName} resulted in an exception", ClientErrorHandling.ConvertException(t.Exception));
                    }
                    cleanup?.Invoke();
                }, CancellationToken.None);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        private static async Task ContinueSubscription<T>( Action<T> newValueReceived, IAsyncStreamReader<T> responseStream, CancellationToken cancellationToken ) where T : class, ISilaTransferObject
        {
            try
            {
                while(await responseStream.MoveNext( cancellationToken ))
                {
                    newValueReceived?.Invoke( responseStream.Current );
                }
            }
            catch(RpcException e)
            {
                if(e.StatusCode != StatusCode.Cancelled || !cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                // operation cancelled is not an exception if we asked for it
            }
        }

        /// <inheritdoc />
        public void ExecuteUnobservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
        {
            ExecuteUnobservableCommand( serviceName, commandName, request, callInfo, ProtobufMarshaller<TRequest>.Default );
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
            ExecuteUnobservableCommand( serviceName, commandName, request, callInfo, ToMarshaller( requestSerializer ) );
        }

        private void ExecuteUnobservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            Marshaller<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject
        {
            var commandMethod = new Method<TRequest, EmptyRequest>( MethodType.Unary, serviceName, commandName, requestSerializer, EmptyRequestMarshaller.Instance );
            CallInvoker.BlockingUnaryCall( commandMethod, null, callInfo.ToCallOptions(), request );
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
            return ExecuteUnobservableCommand( serviceName, commandName, request, callInfo, ProtobufMarshaller<TRequest>.Default, ProtobufMarshaller<TResponse>.Default );
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
            return ExecuteUnobservableCommand( serviceName, commandName, request, callInfo, ToMarshaller( requestSerializer ), ToMarshaller( responseSerializer ) );
        }

        private TResponse ExecuteUnobservableCommand<TRequest, TResponse>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            Marshaller<TRequest> requestSerializer,
            Marshaller<TResponse> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponse : class
        {
            var commandMethod = new Method<TRequest, TResponse>( MethodType.Unary, serviceName, commandName, requestSerializer, responseSerializer );
            return CallInvoker.BlockingUnaryCall( commandMethod, null, callInfo.ToCallOptions(), request );
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
            var commandMethod = new Method<TRequest, EmptyRequest>( MethodType.Unary, serviceName, commandName, ToMarshaller(requestSerializer), EmptyRequestMarshaller.Instance );
            return CallInvoker.AsyncUnaryCall( commandMethod, null, callInfo.ToCallOptions(), request ).ResponseAsync;
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
            var commandMethod = new Method<TRequest, TResponse>( MethodType.Unary, serviceName, commandName, ToMarshaller( requestSerializer ), ToMarshaller( responseSerializer ) );
            return CallInvoker.AsyncUnaryCall( commandMethod, null, callInfo.ToCallOptions(), request ).ResponseAsync;
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
            return ExecuteObservableCommand( serviceName, commandName, request, errorConverter, callInfo, ProtobufMarshaller<TRequest>.Default );
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
            return ExecuteObservableCommand( serviceName, commandName, request, errorConverter, callInfo, ToMarshaller( requestSerializer ) );
        }

        private IObservableCommand ExecuteObservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo,
            Marshaller<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject
        {
            var commandMethod = new Method<TRequest, CommandConfirmation>( MethodType.Unary, serviceName, commandName, requestSerializer, ProtobufMarshaller<CommandConfirmation>.Default );
            var commandStateMethod = new Method<CommandExecutionUuid, ExecutionState>( MethodType.ServerStreaming, serviceName, commandName + "_Info", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<ExecutionState>.Default );
            var resultMethod = new Method<CommandExecutionUuid, EmptyRequest>( MethodType.Unary, serviceName, commandName + "_Result", ProtobufMarshaller<CommandExecutionUuid>.Default, EmptyRequestMarshaller.Instance );

            var confirmation = CallInvoker.BlockingUnaryCall( commandMethod, null,
                callInfo.ToCallOptions(), request );
            var command = new ObservableClientCommand(
                callInfo,
                CallInvoker,
                confirmation.CommandId,
                commandStateMethod,
                resultMethod,
                errorConverter );

            command.Start();
            return command;
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
            return ExecuteObservableCommand( serviceName, commandName, request, responseConverter, errorConverter, callInfo, ProtobufMarshaller<TRequest>.Default, ProtobufMarshaller<TResponseDto>.Default );
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
            return ExecuteObservableCommand( serviceName, commandName, request, responseConverter, errorConverter, callInfo, ToMarshaller( requestSerializer ), ToMarshaller( responseSerializer ) );
        }

        private IObservableCommand<TResponse> ExecuteObservableCommand<TRequest, TResponse, TResponseDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TResponseDto, TResponse> responseConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo,
            Marshaller<TRequest> requestSerializer,
            Marshaller<TResponseDto> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponseDto : class, ISilaTransferObject
        {
            var commandMethod = new Method<TRequest, CommandConfirmation>( MethodType.Unary, serviceName, commandName, requestSerializer, ProtobufMarshaller<CommandConfirmation>.Default );
            var commandStateMethod = new Method<CommandExecutionUuid, ExecutionState>( MethodType.ServerStreaming, serviceName, commandName + "_Info", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<ExecutionState>.Default );
            var resultMethod = new Method<CommandExecutionUuid, TResponseDto>( MethodType.Unary, serviceName, commandName + "_Result", ProtobufMarshaller<CommandExecutionUuid>.Default, responseSerializer );

            var confirmation = CallInvoker.BlockingUnaryCall( commandMethod, null,
                callInfo.ToCallOptions(), request );
            var command = new ObservableClientCommand<TResponseDto, TResponse>(
                callInfo,
                CallInvoker,
                confirmation.CommandId,
                commandStateMethod,
                resultMethod,
                responseConverter,
                errorConverter );

            command.Start();
            return command;
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
            return ExecuteIntermediatesCommand( serviceName, commandName, request, intermediateConverter, errorConverter, callInfo, ProtobufMarshaller<TRequest>.Default, ProtobufMarshaller<TIntermediateDto>.Default );
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
            return ExecuteIntermediatesCommand( serviceName, commandName, request, intermediateConverter, errorConverter, callInfo, ToMarshaller( requestSerializer ), ToMarshaller( intermediateSerializer ) );
        }

        private IIntermediateObservableCommand<TIntermediate> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TIntermediateDto, TIntermediate> intermediateConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo,
            Marshaller<TRequest> requestSerializer,
            Marshaller<TIntermediateDto> intermediateSerializer )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class, ISilaTransferObject
        {
            var commandMethod = new Method<TRequest, CommandConfirmation>( MethodType.Unary, serviceName, commandName, requestSerializer, ProtobufMarshaller<CommandConfirmation>.Default );
            var commandStateMethod = new Method<CommandExecutionUuid, ExecutionState>( MethodType.ServerStreaming, serviceName, commandName + "_Info", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<ExecutionState>.Default );
            var commandIntermediatesMethod = new Method<CommandExecutionUuid, TIntermediateDto>( MethodType.ServerStreaming, serviceName, commandName + "_Intermediate", ProtobufMarshaller<CommandExecutionUuid>.Default, intermediateSerializer );
            var resultMethod = new Method<CommandExecutionUuid, EmptyRequest>( MethodType.Unary, serviceName, commandName + "_Result", ProtobufMarshaller<CommandExecutionUuid>.Default, EmptyRequestMarshaller.Instance );

            var confirmation = CallInvoker.BlockingUnaryCall( commandMethod, null,
                callInfo.ToCallOptions(), request );
            var command = new ObservableIntermediateClientCommand<TIntermediateDto, TIntermediate>(
                callInfo,
                CallInvoker,
                confirmation.CommandId,
                commandStateMethod,
                resultMethod,
                commandIntermediatesMethod,
                intermediateConverter,
                errorConverter );

            command.Start();
            return command;
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
            return ExecuteIntermediatesCommand( serviceName, commandName, request, intermediateConverter, responseConverter, errorConverter, callInfo,
                ProtobufMarshaller<TRequest>.Default, ProtobufMarshaller<TIntermediateDto>.Default, ProtobufMarshaller<TResponseDto>.Default );
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
            return ExecuteIntermediatesCommand( serviceName, commandName, request, intermediateConverter, responseConverter, errorConverter, callInfo,
                ToMarshaller(requestSerializer), ToMarshaller(intermediateSerializer), ToMarshaller(responseSerializer) );
        }

        private IIntermediateObservableCommand<TIntermediate, TResponse> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto, TResponse, TResponseDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TIntermediateDto, TIntermediate> intermediateConverter,
            Func<TResponseDto, TResponse> responseConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo,
            Marshaller<TRequest> requestSerializer,
            Marshaller<TIntermediateDto> intermediateSerializer,
            Marshaller<TResponseDto> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class, ISilaTransferObject
            where TResponseDto : class, ISilaTransferObject
        {
            var commandMethod = new Method<TRequest, CommandConfirmation>( MethodType.Unary, serviceName, commandName, requestSerializer, ProtobufMarshaller<CommandConfirmation>.Default );
            var commandStateMethod = new Method<CommandExecutionUuid, ExecutionState>( MethodType.ServerStreaming, serviceName, commandName + "_Info", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<ExecutionState>.Default );
            var commandIntermediatesMethod = new Method<CommandExecutionUuid, TIntermediateDto>( MethodType.ServerStreaming, serviceName, commandName + "_Intermediate", ProtobufMarshaller<CommandExecutionUuid>.Default, intermediateSerializer );
            var resultMethod = new Method<CommandExecutionUuid, TResponseDto>( MethodType.Unary, serviceName, commandName + "_Result", ProtobufMarshaller<CommandExecutionUuid>.Default, responseSerializer );

            var confirmation = CallInvoker.BlockingUnaryCall( commandMethod, null,
                callInfo.ToCallOptions(), request );
            var command = new ObservableIntermediateClientCommand<TIntermediateDto, TIntermediate, TResponseDto, TResponse>( 
                callInfo, 
                CallInvoker, 
                confirmation.CommandId, 
                commandStateMethod, 
                commandIntermediatesMethod, 
                resultMethod, 
                intermediateConverter, 
                responseConverter, 
                errorConverter );

            command.Start();
            return command;
        }

        private static Marshaller<T> ToMarshaller<T>(ByteSerializer<T> byteSerializer)
        {
            return new Marshaller<T>( byteSerializer.Serializer, byteSerializer.Deserializer );
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAffected( string serviceName, string metadataName, string metadataIdentifier )
        {
            var method = new Method<EmptyRequest, MetadataAffectedResponse>( MethodType.Unary, serviceName,
                $"Get_FCPAffectedByMetadata_{metadataName}", EmptyRequestMarshaller.Instance, ProtobufMarshaller<MetadataAffectedResponse>.Default );

            var result = CallInvoker.BlockingUnaryCall( method, null, new CallOptions(), EmptyRequest.Instance );
            return result.Affected.Select( dto => dto.Value );
        }

        /// <inheritdoc />
        public IBinaryStore CreateBinaryStore( string commandParameterIdentifier, IClientExecutionManager executionManager )
        {
            return new ClientBinaryStore( commandParameterIdentifier, CallInvoker, executionManager );
        }

        /// <inheritdoc />
        public Exception ConvertException( Exception ex,
            Func<string, string, Exception> executionErrorConversion = null )
        {
            return ClientErrorHandling.ConvertException( ex, executionErrorConversion );
        }

        public async void Dispose()
        {
            await _channel.ShutdownAsync();
        }
    }
}
