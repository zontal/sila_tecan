using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Grpc.Core;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class ServerBuilder : IServerBuilder
    {
        private readonly SiLAServer _server;
        private readonly Feature _feature;
        private readonly ServerPoolDispatcher _dispatcher;
        private readonly ServiceBinderBase _serviceDefinitionBuilder;

        public ServerBuilder(SiLAServer server, Feature feature, ServerPoolDispatcher dispatcher, ServiceBinderBase serviceBinder)
        {
            _server = server;
            _feature = feature;
            _serviceDefinitionBuilder = serviceBinder;
            _dispatcher = dispatcher;
        }

        private string ServiceName => _feature.Namespace + "." + _feature.Identifier;

        public void RegisterMetadata( string metadata, IRequestInterceptor requestInterceptor )
        {
            var method = new Method<EmptyRequest, List<StringDto>>( MethodType.Unary, ServiceName, "Get_FCPAffectedByMetadata_" + metadata, EmptyRequestMarshaller.Instance, ProtobufMarshaller<List<StringDto>>.Default);
            var builder = new MetadataHandler( _server, requestInterceptor );
            _serviceDefinitionBuilder.AddMethod( method, builder.GetAffectedBy );
            _dispatcher.RegisterHandler( _feature.FullyQualifiedIdentifier + "/Metadata/" + metadata, builder );
        }

        public void RegisterUnobservableProperty<T>( string propertyName, Func<T> implementation ) where T : class
        {
            RegisterUnobservableProperty( propertyName, implementation, ProtobufMarshaller<T>.Default );
        }

        public void RegisterUnobservableProperty<T>( string propertyName, Func<T> implementation, ByteSerializer<T> propertySerializer ) where T : class
        {
            RegisterUnobservableProperty( propertyName, implementation, ProtobufMarshaller<T>.FromByteSerializer(propertySerializer) );
        }

        public void RegisterUnobservableProperty<T>( string propertyName, Func<T> implementation, Marshaller<T> propertySerializer ) where T : class
        {
            var method = new Method<EmptyRequest, T>( MethodType.Unary, ServiceName, "Get_" + propertyName, EmptyRequestMarshaller.Instance, propertySerializer );
            var property = _feature.Items.OfType<FeatureProperty>().FirstOrDefault( p => p.Identifier == propertyName );
            var builder = new UnobservablePropertyHandler<T>( _server, _feature.GetFullyQualifiedIdentifier( property ), implementation );
            _serviceDefinitionBuilder.AddMethod( method, builder.Execute );
            _dispatcher.RegisterHandler( _feature.FullyQualifiedIdentifier + "/Property/" + propertyName, builder );
        }

        public void RegisterObservableProperty<T>( string propertyName, Func<T> implementation, object changeSource, string clientPropertyName = null ) where T : class
        {
            RegisterObservableProperty( propertyName, implementation, changeSource, ProtobufMarshaller<T>.Default, clientPropertyName );
        }

        public void RegisterObservableProperty<T>( string propertyName, Func<T> implementation, object changeSource, ByteSerializer<T> propertySerializer, string clientPropertyName = null ) where T : class
        {
            RegisterObservableProperty( propertyName, implementation, changeSource, ProtobufMarshaller<T>.FromByteSerializer(propertySerializer), clientPropertyName );
        }

        private void RegisterObservableProperty<T>( string propertyName, Func<T> implementation, object changeSource, Marshaller<T> propertySerializer, string clientPropertyName ) where T : class
        {
            var method = new Method<EmptyRequest, T>( MethodType.Unary, ServiceName, "Subscribe_" + propertyName, EmptyRequestMarshaller.Instance, propertySerializer );
            var property = _feature.Items.OfType<FeatureProperty>().FirstOrDefault( p => p.Identifier == propertyName );
            var builder = new ObservablePropertyHandler<T>( _server, _feature.GetFullyQualifiedIdentifier( property ), implementation, changeSource as INotifyPropertyChanged, clientPropertyName ?? propertyName );
            _serviceDefinitionBuilder.AddMethod( method, builder.Subscribe );
            _dispatcher.RegisterHandler( _feature.FullyQualifiedIdentifier + "/Property/" + propertyName, builder );
        }

        public void RegisterUnobservableCommand<TRequest, TResponse>( string methodName, Func<TRequest, TResponse> implementation )
            where TRequest : class, ISilaRequestObject
            where TResponse : class
        {
            RegisterUnobservableCommand( methodName, implementation, ProtobufMarshaller<TRequest>.Default, ProtobufMarshaller<TResponse>.Default );
        }

        public void RegisterUnobservableCommand<TRequest, TResponse>( string methodName, Func<TRequest, TResponse> implementation, ByteSerializer<TRequest> requestSerializer, ByteSerializer<TResponse> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponse : class
        {
            RegisterUnobservableCommand( methodName, implementation, ProtobufMarshaller<TRequest>.FromByteSerializer(requestSerializer), ProtobufMarshaller<TResponse>.FromByteSerializer(responseSerializer) );
        }

        private void RegisterUnobservableCommand<TRequest, TResponse>( string methodName, Func<TRequest, TResponse> implementation, Marshaller<TRequest> requestSerializer, Marshaller<TResponse> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponse : class
        {
            var method = new Method<TRequest, TResponse>( MethodType.Unary, ServiceName, methodName, requestSerializer, responseSerializer );
            var builder = new UnobservableCommandHandler<TRequest, TResponse>( _server, implementation );
            _serviceDefinitionBuilder.AddMethod( method, builder.Execute );
            _dispatcher.RegisterHandler( _feature.FullyQualifiedIdentifier + "/Command/" + methodName, builder );
        }

        public void RegisterObservableCommand<TRequest>( string methodName, Func<TRequest, IObservableCommand> implementation, Func<Exception, Exception> exceptionConverter )
            where TRequest : class, ISilaRequestObject
        {
            RegisterObservableCommandCore( methodName, implementation, exceptionConverter, ProtobufMarshaller<TRequest>.Default, null );
        }

        public void RegisterObservableCommand<TRequest>( string methodName, Func<TRequest, IObservableCommand> implementation, Func<Exception, Exception> exceptionConverter, ByteSerializer<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject
        {
            RegisterObservableCommandCore( methodName, implementation, exceptionConverter, ProtobufMarshaller<TRequest>.FromByteSerializer(requestSerializer), null );
        }

        private void RegisterObservableCommandCore<TRequest>( string methodName, Func<TRequest, IObservableCommand> implementation, Func<Exception, Exception> exceptionConverter, Marshaller<TRequest> requestSerializer, IIntermediateRequestHandler intermediateHandler )
            where TRequest : class, ISilaRequestObject
        {
            var method = new Method<TRequest, CommandConfirmation>( MethodType.Unary, ServiceName, methodName, requestSerializer, ProtobufMarshaller<CommandConfirmation>.Default );
            var stateMethod = new Method<CommandExecutionUuid, ExecutionState>( MethodType.ServerStreaming, ServiceName, methodName + "_Info", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<ExecutionState>.Default );
            var resultMethod = new Method<CommandExecutionUuid, EmptyRequest>( MethodType.Unary, ServiceName, methodName + "_Result", ProtobufMarshaller<CommandExecutionUuid>.Default, EmptyRequestMarshaller.Instance );
            var builder = new ObservableCommandHandler<TRequest>( _server, implementation, exceptionConverter, intermediateHandler );
            _serviceDefinitionBuilder.AddMethod( method, builder.Execute );
            _serviceDefinitionBuilder.AddMethod( stateMethod, builder.QueryState );
            _serviceDefinitionBuilder.AddMethod( resultMethod, builder.GetResponse );
            _dispatcher.RegisterHandler( _feature.FullyQualifiedIdentifier + "/Command/" + methodName, builder );
        }

        public void RegisterObservableCommand<TRequest, TResponse, TResponseDto>( string methodName, Func<TRequest, IObservableCommand<TResponse>> implementation, Func<TResponse, TResponseDto> responseConverter, Func<Exception, Exception> exceptionConverter )
            where TRequest : class, ISilaRequestObject
            where TResponseDto : class
        {
            RegisterObservableCommandCore( methodName, implementation, responseConverter, exceptionConverter, ProtobufMarshaller<TRequest>.Default, ProtobufMarshaller<TResponseDto>.Default, null );
        }

        void IServerBuilder.RegisterObservableCommand<TRequest, TResponse, TResponseDto>( string methodName, Func<TRequest, IObservableCommand<TResponse>> implementation, Func<TResponse, TResponseDto> responseConverter, Func<Exception, Exception> exceptionConverter, ByteSerializer<TRequest> requestSerializer, ByteSerializer<TResponseDto> responseSerializer )
        {
            RegisterObservableCommandCore( methodName, implementation, responseConverter, exceptionConverter, ProtobufMarshaller<TRequest>.FromByteSerializer(requestSerializer), ProtobufMarshaller<TResponseDto>.FromByteSerializer(responseSerializer), null );
        }


        private void RegisterObservableCommandCore<TRequest, TResponse, TResponseDto>( string methodName, Func<TRequest, IObservableCommand<TResponse>> implementation, Func<TResponse, TResponseDto> responseConverter, Func<Exception, Exception> exceptionConverter, Marshaller<TRequest> requestSerializer, Marshaller<TResponseDto> responseSerializer, IIntermediateRequestHandler intermediateHandler )
            where TRequest : class, ISilaRequestObject
            where TResponseDto : class
        {
            var method = new Method<TRequest, CommandConfirmation>( MethodType.Unary, ServiceName, methodName, ProtobufMarshaller<TRequest>.Default, ProtobufMarshaller<CommandConfirmation>.Default );
            var stateMethod = new Method<CommandExecutionUuid, ExecutionState>( MethodType.ServerStreaming, ServiceName, methodName + "_Info", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<ExecutionState>.Default );
            var resultMethod = new Method<CommandExecutionUuid, TResponseDto>( MethodType.Unary, ServiceName, methodName + "_Result", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<TResponseDto>.Default );
            var builder = new ObservableCommandHandler<TRequest, TResponse, TResponseDto>( _server, implementation, responseConverter, exceptionConverter, intermediateHandler );
            _serviceDefinitionBuilder.AddMethod( method, builder.Execute );
            _serviceDefinitionBuilder.AddMethod( stateMethod, builder.QueryState );
            _serviceDefinitionBuilder.AddMethod( resultMethod, builder.GetCastedResponse );
            _dispatcher.RegisterHandler( _feature.FullyQualifiedIdentifier + "/Command/" + methodName, builder );
        }

        public void RegisterCommandWithIntermediates<TRequest, TIntermediate, TIntermediateDto>( string commandName, Func<TRequest, IIntermediateObservableCommand<TIntermediate>> implementation, Func<TIntermediate, TIntermediateDto> intermediateConverter, Func<Exception, Exception> exceptionConverter )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class
        {
            var intermediateResults = new Method<CommandExecutionUuid, TIntermediateDto>( MethodType.ServerStreaming, ServiceName, commandName + "_Intermediate", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<TIntermediateDto>.Default );
            var builder = new IntermediateCommandHandler<TIntermediate, TIntermediateDto>( _server, intermediateConverter, exceptionConverter );
            RegisterObservableCommandCore( commandName, implementation, exceptionConverter, ProtobufMarshaller<TRequest>.Default, builder );
            _serviceDefinitionBuilder.AddMethod( intermediateResults, builder.GetIntermediateResponses );
        }

        public void RegisterCommandWithIntermediates<TRequest, TIntermediate, TIntermediateDto>( string commandName, Func<TRequest, IIntermediateObservableCommand<TIntermediate>> implementation, Func<TIntermediate, TIntermediateDto> intermediateConverter, Func<Exception, Exception> exceptionConverter, ByteSerializer<TRequest> requestSerializer, ByteSerializer<TIntermediateDto> intermediatesSerializer )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class
        {
            var intermediateResults = new Method<CommandExecutionUuid, TIntermediateDto>( MethodType.ServerStreaming, ServiceName, commandName + "_Intermediate", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<TIntermediateDto>.FromByteSerializer(intermediatesSerializer) );
            var builder = new IntermediateCommandHandler<TIntermediate, TIntermediateDto>( _server, intermediateConverter, exceptionConverter );
            RegisterObservableCommandCore( commandName, implementation, exceptionConverter, ProtobufMarshaller<TRequest>.FromByteSerializer(requestSerializer), builder );
            _serviceDefinitionBuilder.AddMethod( intermediateResults, builder.GetIntermediateResponses );
        }

        public void RegisterCommandWithIntermediates<TRequest, TIntermediate, TIntermediateDto, TResponse, TResponseDto>( string commandName, Func<TRequest, IIntermediateObservableCommand<TIntermediate, TResponse>> implementation, Func<TIntermediate, TIntermediateDto> intermediateConverter,
            Func<TResponse, TResponseDto> responseConverter, Func<Exception, Exception> exceptionConverter )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class 
            where TResponseDto : class
        {
            var intermediateResults = new Method<CommandExecutionUuid, TIntermediateDto>( MethodType.ServerStreaming, ServiceName, commandName + "_Intermediate", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<TIntermediateDto>.Default );
            var builder = new IntermediateCommandHandler<TIntermediate, TIntermediateDto>( _server, intermediateConverter, exceptionConverter );
            RegisterObservableCommandCore( commandName, implementation, responseConverter, exceptionConverter, ProtobufMarshaller<TRequest>.Default, ProtobufMarshaller<TResponseDto>.Default, builder );
            _serviceDefinitionBuilder.AddMethod( intermediateResults, builder.GetIntermediateResponses );
        }

        public void RegisterCommandWithIntermediates<TRequest, TIntermediate, TIntermediateDto, TResponse, TResponseDto>( string commandName, Func<TRequest, IIntermediateObservableCommand<TIntermediate, TResponse>> implementation, Func<TIntermediate, TIntermediateDto> intermediateConverter, Func<TResponse, TResponseDto> responseConverter, Func<Exception, Exception> exceptionConverter, ByteSerializer<TRequest> requestSerializer, ByteSerializer<TIntermediateDto> intermediatesSerializer, ByteSerializer<TResponseDto> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class
            where TResponseDto : class
        {
            var intermediateResults = new Method<CommandExecutionUuid, TIntermediateDto>( MethodType.ServerStreaming, ServiceName, commandName + "_Intermediate", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<TIntermediateDto>.FromByteSerializer(intermediatesSerializer) );
            var builder = new IntermediateCommandHandler<TIntermediate, TIntermediateDto>( _server, intermediateConverter, exceptionConverter );
            RegisterObservableCommandCore( commandName, implementation, responseConverter, exceptionConverter, ProtobufMarshaller<TRequest>.FromByteSerializer(requestSerializer), ProtobufMarshaller<TResponseDto>.FromByteSerializer(responseSerializer), builder );
            _serviceDefinitionBuilder.AddMethod( intermediateResults, builder.GetIntermediateResponses );
        }
    }
}
