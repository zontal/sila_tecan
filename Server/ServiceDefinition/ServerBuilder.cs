using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Grpc.Core;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class ServerBuilder : IServerBuilder
    {
        private readonly SiLAServer _server;
        private readonly Feature _feature;
        private readonly ServerServiceDefinition.Builder _serviceDefinitionBuilder;

        public ServerBuilder(SiLAServer server, Feature feature)
        {
            _server = server;
            _feature = feature;
            _serviceDefinitionBuilder = ServerServiceDefinition.CreateBuilder();
        }

        private string ServiceName => _feature.Namespace + "." + _feature.Identifier;

        public void RegisterMetadata( string metadata, IRequestInterceptor requestInterceptor )
        {
            var method = new Method<EmptyRequest, List<StringDto>>( MethodType.Unary, ServiceName, "Get_FCPAffectedByMetadata_" + metadata, EmptyRequestMarshaller.Instance, ProtobufMarshaller<List<StringDto>>.Default);
            var builder = new MetadataHandler( _server, requestInterceptor );
            _serviceDefinitionBuilder.AddMethod( method, builder.GetAffectedBy );
        }

        public void RegisterUnobservableProperty<T>( string propertyName, Func<T> implementation ) where T : class
        {
            var method = new Method<EmptyRequest, T>(MethodType.Unary, ServiceName, "Get_" + propertyName, EmptyRequestMarshaller.Instance, ProtobufMarshaller<T>.Default );
            var property = _feature.Items.OfType<FeatureProperty>().FirstOrDefault( p => p.Identifier == propertyName );
            var builder = new UnobservablePropertyHandler<T>(_server, _feature.GetFullyQualifiedIdentifier(property), implementation);
            _serviceDefinitionBuilder.AddMethod( method, builder.Execute );
        }

        public void RegisterObservableProperty<T>( string propertyName, Func<T> implementation, object changeSource, string clientPropertyName = null ) where T : class
        {
            var method = new Method<EmptyRequest, T>( MethodType.Unary, ServiceName, "Subscribe_" + propertyName, EmptyRequestMarshaller.Instance, ProtobufMarshaller<T>.Default );
            var property = _feature.Items.OfType<FeatureProperty>().FirstOrDefault( p => p.Identifier == propertyName );
            var builder = new ObservablePropertyHandler<T>( _server, _feature.GetFullyQualifiedIdentifier( property ), implementation, changeSource as INotifyPropertyChanged, clientPropertyName ?? propertyName );
            _serviceDefinitionBuilder.AddMethod( method, builder.Subscribe );
        }

        public void RegisterUnobservableCommand<TRequest, TResponse>( string methodName, Func<TRequest, TResponse> implementation )
            where TRequest : class, ISilaRequestObject
            where TResponse : class
        {
            var method = new Method<TRequest, TResponse>( MethodType.Unary, ServiceName, methodName, ProtobufMarshaller<TRequest>.Default, ProtobufMarshaller<TResponse>.Default );
            var builder = new UnobservableCommandHandler<TRequest, TResponse>( _server, implementation );
            _serviceDefinitionBuilder.AddMethod( method, builder.Execute );
        }

        public void RegisterObservableCommand<TRequest>( string methodName, Func<TRequest, IObservableCommand> implementation, Func<Exception, Exception> exceptionConverter )
            where TRequest : class, ISilaRequestObject
        {
            var method = new Method<TRequest, CommandConfirmation>( MethodType.Unary, ServiceName, methodName, ProtobufMarshaller<TRequest>.Default, ProtobufMarshaller<CommandConfirmation>.Default );
            var stateMethod = new Method<CommandExecutionUuid, ExecutionState>( MethodType.ServerStreaming, ServiceName, methodName + "_Info", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<ExecutionState>.Default );
            var resultMethod = new Method<CommandExecutionUuid, EmptyRequest>( MethodType.Unary, ServiceName, methodName + "_Result", ProtobufMarshaller<CommandExecutionUuid>.Default, EmptyRequestMarshaller.Instance );
            var builder = new ObservableCommandHandler<TRequest>( _server, implementation, exceptionConverter );
            _serviceDefinitionBuilder.AddMethod( method, builder.Execute );
            _serviceDefinitionBuilder.AddMethod( stateMethod, builder.QueryState );
            _serviceDefinitionBuilder.AddMethod( resultMethod, builder.GetResponse );
        }

        public void RegisterObservableCommand<TRequest, TResponse, TResponseDto>( string methodName, Func<TRequest, IObservableCommand<TResponse>> implementation, Func<TResponse, TResponseDto> responseConverter, Func<Exception, Exception> exceptionConverter )
            where TRequest : class, ISilaRequestObject
            where TResponseDto : class
        {
            var method = new Method<TRequest, CommandConfirmation>( MethodType.Unary, ServiceName, methodName, ProtobufMarshaller<TRequest>.Default, ProtobufMarshaller<CommandConfirmation>.Default );
            var stateMethod = new Method<CommandExecutionUuid, ExecutionState>( MethodType.ServerStreaming, ServiceName, methodName + "_Info", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<ExecutionState>.Default );
            var resultMethod = new Method<CommandExecutionUuid, TResponseDto>( MethodType.Unary, ServiceName, methodName + "_Result", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<TResponseDto>.Default );
            var builder = new ObservableCommandHandler<TRequest, TResponse, TResponseDto>( _server, implementation, responseConverter, exceptionConverter );
            _serviceDefinitionBuilder.AddMethod( method, builder.Execute );
            _serviceDefinitionBuilder.AddMethod( stateMethod, builder.QueryState );
            _serviceDefinitionBuilder.AddMethod( resultMethod, builder.GetCastedResponse );
        }

        public void RegisterCommandWithIntermediates<TRequest, TIntermediate, TIntermediateDto>( string commandName, Func<TRequest, IIntermediateObservableCommand<TIntermediate>> implementation, Func<TIntermediate, TIntermediateDto> intermediateConverter, Func<Exception, Exception> exceptionConverter )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class
        {
            RegisterObservableCommand<TRequest>( commandName, implementation, exceptionConverter );
            var intermediateResults = new Method<CommandExecutionUuid, TIntermediateDto>( MethodType.ServerStreaming, ServiceName, commandName + "_IntermediateResults", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<TIntermediateDto>.Default );
            var builder = new IntermediateCommandHandler<TIntermediate, TIntermediateDto>( _server, intermediateConverter, exceptionConverter );
            _serviceDefinitionBuilder.AddMethod( intermediateResults, builder.GetIntermediateResponses );
        }

        public void RegisterCommandWithIntermediates<TRequest, TIntermediate, TIntermediateDto, TResponse, TResponseDto>( string commandName, Func<TRequest, IIntermediateObservableCommand<TIntermediate, TResponse>> implementation, Func<TIntermediate, TIntermediateDto> intermediateConverter,
            Func<TResponse, TResponseDto> responseConverter, Func<Exception, Exception> exceptionConverter )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class 
            where TResponseDto : class
        {
            RegisterObservableCommand<TRequest, TResponse, TResponseDto>( commandName, implementation, responseConverter, exceptionConverter );
            var intermediateResults = new Method<CommandExecutionUuid, TIntermediateDto>( MethodType.ServerStreaming, ServiceName, commandName + "_IntermediateResults", ProtobufMarshaller<CommandExecutionUuid>.Default, ProtobufMarshaller<TIntermediateDto>.Default );
            var builder = new IntermediateCommandHandler<TIntermediate, TIntermediateDto>( _server, intermediateConverter, exceptionConverter );
            _serviceDefinitionBuilder.AddMethod( intermediateResults, builder.GetIntermediateResponses );
        }

        public ServerServiceDefinition CreateServiceDefinition()
        {
            return _serviceDefinitionBuilder.Build();
        }
    }
}
