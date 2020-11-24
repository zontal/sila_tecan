using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Discovery
{
    internal class GrpcChannel : ClientBase<GrpcChannel>, IClientChannel
    {

        public GrpcChannel( Channel channel ) : base( channel ) { }

        public GrpcChannel(CallInvoker callInvoker) : base(callInvoker) { }

        protected GrpcChannel(ClientBaseConfiguration configuration) : base(configuration) { }


        protected override GrpcChannel NewInstance( ClientBaseConfiguration configuration )
        {
            return new GrpcChannel( configuration );
        }
        public T ReadProperty<T>( string serviceName, string propertyName, IClientCallInfo callInfo ) where T : class, ISilaTransferObject
        {
            return ReadProperty( serviceName, propertyName, callInfo, ProtobufMarshaller<T>.Default );
        }

        public T ReadProperty<T>( string serviceName, string propertyName, IClientCallInfo callInfo, ByteSerializer<T> serializer ) where T : class, ISilaTransferObject
        {
            return ReadProperty( serviceName, propertyName, callInfo, ToMarshaller(serializer) );
        }

        private T ReadProperty<T>( string serviceName, string propertyName, IClientCallInfo callInfo, Marshaller<T> serializer ) where T : class, ISilaTransferObject
        {
            var propertyMethod = new Method<EmptyRequest, T>( MethodType.Unary, serviceName, "Get_" + propertyName, EmptyRequest.Marshaller, serializer );
            return CallInvoker.BlockingUnaryCall( propertyMethod, null, callInfo.ToCallOptions(), EmptyRequest.Instance );
        }

        public Task<T> ReadPropertyAsync<T>( string serviceName, string propertyName, IClientCallInfo callInfo, ByteSerializer<T> serializer ) where T : class, ISilaTransferObject
        {
            var propertyMethod = new Method<EmptyRequest, T>( MethodType.Unary, serviceName, "Get_" + propertyName, EmptyRequest.Marshaller, ToMarshaller(serializer) );
            return CallInvoker.AsyncUnaryCall( propertyMethod, null, callInfo.ToCallOptions(), EmptyRequest.Instance ).ResponseAsync;
        }

        public Task SubscribeProperty<T>( string serviceName, string propertyName, Action<T> newValueReceived, IClientCallInfo callInfo, CancellationToken cancellationToken = default ) where T : class, ISilaTransferObject
        {
            return SubscribeProperty( serviceName, propertyName, newValueReceived, callInfo, ProtobufMarshaller<T>.Default, cancellationToken );
        }

        public Task SubscribeProperty<T>( string serviceName, string propertyName, Action<T> newValueReceived, IClientCallInfo callInfo, ByteSerializer<T> serializer, CancellationToken cancellationToken = default ) where T : class, ISilaTransferObject
        {
            return SubscribeProperty( serviceName, propertyName, newValueReceived, callInfo, ToMarshaller( serializer ), cancellationToken );
        }

        private async Task SubscribeProperty<T>( string serviceName, string propertyName, Action<T> newValueReceived, IClientCallInfo callInfo, Marshaller<T> serializer, CancellationToken cancellationToken ) where T : class, ISilaTransferObject
        {
            var subscribeMethod = new Method<EmptyRequest, T>( MethodType.ServerStreaming, serviceName, "Subscribe_" + propertyName, EmptyRequest.Marshaller, serializer );

            var asyncCall = CallInvoker.AsyncServerStreamingCall( subscribeMethod, null,
                callInfo.ToCallOptions().WithCancellationToken(cancellationToken), EmptyRequest.Instance );
            var responseStream = asyncCall.ResponseStream;

            try
            {
                while(await responseStream.MoveNext( default ))
                {
                    newValueReceived?.Invoke( responseStream.Current );
                }
                callInfo.FinishSuccessful();
            }
            catch(Exception exception)
            {
                callInfo.FinishWithErrors( exception );
                throw;
            }
        }

        public void ExecuteUnobservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
        {
            ExecuteUnobservableCommand( serviceName, commandName, request, callInfo, ProtobufMarshaller<TRequest>.Default );
        }

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
            var commandMethod = new Method<TRequest, EmptyRequest>( MethodType.Unary, serviceName, commandName, requestSerializer, EmptyRequest.Marshaller );
            CallInvoker.BlockingUnaryCall( commandMethod, null, callInfo.ToCallOptions(), request );
        }

        public Task ExecuteUnobservableCommandAsync<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject
        {
            var commandMethod = new Method<TRequest, EmptyRequest>( MethodType.Unary, serviceName, commandName, ToMarshaller(requestSerializer), EmptyRequest.Marshaller );
            return CallInvoker.AsyncUnaryCall( commandMethod, null, callInfo.ToCallOptions(), request ).ResponseAsync;
        }

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
            var commandStateMethod = new Method<CommandExecutionUuid, ExecutionState>( MethodType.ServerStreaming, serviceName, commandName + "_State", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<ExecutionState>.Default );
            
            var confirmation = CallInvoker.BlockingUnaryCall( commandMethod, null,
                callInfo.ToCallOptions(), request );
            var command = new ObservableClientCommand(
                callInfo,
                CallInvoker,
                confirmation.CommandId,
                commandStateMethod,
                errorConverter );

            command.Start();
            return command;
        }

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
            var commandStateMethod = new Method<CommandExecutionUuid, ExecutionState>( MethodType.ServerStreaming, serviceName, commandName + "_State", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<ExecutionState>.Default );
            var resultMethod = new Method<CommandExecutionUuid, TResponseDto>( MethodType.Unary, serviceName, commandName, ProtobufMarshaller<CommandExecutionUuid>.Default, responseSerializer );

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
            var commandStateMethod = new Method<CommandExecutionUuid, ExecutionState>( MethodType.ServerStreaming, serviceName, commandName + "_State", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<ExecutionState>.Default );
            var commandIntermediatesMethod = new Method<CommandExecutionUuid, TIntermediateDto>( MethodType.ServerStreaming, serviceName, commandName + "_IntermediateResults", ProtobufMarshaller<CommandExecutionUuid>.Default, intermediateSerializer );
            
            var confirmation = CallInvoker.BlockingUnaryCall( commandMethod, null,
                callInfo.ToCallOptions(), request );
            var command = new ObservableIntermediateClientCommand<TIntermediateDto, TIntermediate>(
                callInfo,
                CallInvoker,
                confirmation.CommandId,
                commandStateMethod,
                commandIntermediatesMethod,
                intermediateConverter,
                errorConverter );

            command.Start();
            return command;
        }

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
            var commandStateMethod = new Method<CommandExecutionUuid, ExecutionState>( MethodType.ServerStreaming, serviceName, commandName + "_State", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<ExecutionState>.Default );
            var commandIntermediatesMethod = new Method<CommandExecutionUuid, TIntermediateDto>( MethodType.ServerStreaming, serviceName, commandName + "_IntermediateResults", ProtobufMarshaller<CommandExecutionUuid>.Default, intermediateSerializer );
            var resultMethod = new Method<CommandExecutionUuid, TResponseDto>( MethodType.Unary, serviceName, commandName, ProtobufMarshaller<CommandExecutionUuid>.Default, responseSerializer );

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

        public IEnumerable<string> GetAffected( string serviceName, string metadataIdentifier )
        {
            var method = new Method<EmptyRequest, MetadataAffectedResponse>( MethodType.Unary, serviceName,
                $"Get_FCPAffectedByMetadata_{metadataIdentifier}", EmptyRequest.Marshaller, ProtobufMarshaller<MetadataAffectedResponse>.Default );

            var result = CallInvoker.BlockingUnaryCall( method, null, new CallOptions(), EmptyRequest.Instance );
            return result.Affected.Select( dto => dto.Value );
        }

        public IBinaryStore CreateBinaryStore( string commandParameterIdentifier, IClientExecutionManager executionManager )
        {
            return new ClientBinaryStore( commandParameterIdentifier, CallInvoker, executionManager );
        }
    }
}
