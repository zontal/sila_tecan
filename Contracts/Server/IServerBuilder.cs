using System;
using System.ComponentModel;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes an interface for a component that adds service registrations
    /// </summary>
    public interface IServerBuilder
    {
        /// <summary>
        /// Registers the given request interceptor for the given metadata
        /// </summary>
        /// <param name="metadata">The local identifier of the metadata</param>
        /// <param name="requestInterceptor">The interceptor that should be used to handle calls with the given metadata attached to it</param>
        void RegisterMetadata( string metadata, IRequestInterceptor requestInterceptor );

        /// <summary>
        /// Registers that the given unobservable property should be exposed
        /// </summary>
        /// <typeparam name="T">The data transfer type of the property</typeparam>
        /// <param name="propertyName">The local identifier of the property</param>
        /// <param name="implementation">The implementation of the property, i.e. the getter</param>
        void RegisterUnobservableProperty<T>( string propertyName, Func<T> implementation ) where T : class;

        /// <summary>
        /// Registers that the given unobservable property should be exposed
        /// </summary>
        /// <typeparam name="T">The data transfer type of the property</typeparam>
        /// <param name="propertyName">The local identifier of the property</param>
        /// <param name="implementation">The implementation of the property, i.e. the getter</param>
        /// <param name="propertySerializer">A serializer for the property type</param>
        void RegisterUnobservableProperty<T>( string propertyName, Func<T> implementation, ByteSerializer<T> propertySerializer ) where T : class;

        /// <summary>
        /// Registers the given observable property
        /// </summary>
        /// <typeparam name="T">The data transfer type of the property</typeparam>
        /// <param name="propertyName">The local identifier of the property</param>
        /// <param name="implementation">The implementation of the property, i.e. the getter</param>
        /// <param name="changeSource">The source for changes</param>
        /// <param name="clientPropertyName">The client property name</param>
        void RegisterObservableProperty<T>( string propertyName, Func<T> implementation, object changeSource, string clientPropertyName = null ) where T : class;

        /// <summary>
        /// Registers the given observable property
        /// </summary>
        /// <typeparam name="T">The data transfer type of the property</typeparam>
        /// <param name="propertyName">The local identifier of the property</param>
        /// <param name="implementation">The implementation of the property, i.e. the getter</param>
        /// <param name="changeSource">The source for changes</param>
        /// <param name="clientPropertyName">The client property name</param>
        /// <param name="propertySerializer">A serializer for the property type</param>
        void RegisterObservableProperty<T>( string propertyName, Func<T> implementation, object changeSource, ByteSerializer<T> propertySerializer, string clientPropertyName = null ) where T : class;

        /// <summary>
        /// Registers the given unobservable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TResponse">The data transfer type of the response</typeparam>
        /// <param name="methodName">The local identifier of the command</param>
        /// <param name="implementation">The implementation of the command</param>
        void RegisterUnobservableCommand<TRequest, TResponse>( string methodName, Func<TRequest, TResponse> implementation )
            where TRequest : class, ISilaRequestObject
            where TResponse : class;

        /// <summary>
        /// Registers the given unobservable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TResponse">The data transfer type of the response</typeparam>
        /// <param name="methodName">The local identifier of the command</param>
        /// <param name="implementation">The implementation of the command</param>
        /// <param name="requestSerializer">A serializer for the request data</param>
        /// <param name="responseSerializer">A serializer for the response data</param>
        void RegisterUnobservableCommand<TRequest, TResponse>( string methodName, Func<TRequest, TResponse> implementation, ByteSerializer<TRequest> requestSerializer, ByteSerializer<TResponse> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponse : class;

        /// <summary>
        /// Registers the given observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <param name="methodName">The local identifier of the command</param>
        /// <param name="implementation">The implementation of the command</param>
        /// <param name="exceptionConverter">A function that converts exceptions of the command</param>
        void RegisterObservableCommand<TRequest>( string methodName, Func<TRequest, IObservableCommand> implementation, Func<Exception, Exception> exceptionConverter )
            where TRequest : class, ISilaRequestObject;

        /// <summary>
        /// Registers the given observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <param name="methodName">The local identifier of the command</param>
        /// <param name="implementation">The implementation of the command</param>
        /// <param name="exceptionConverter">A function that converts exceptions of the command</param>
        /// <param name="requestSerializer">A serializer for the request data</param>
        void RegisterObservableCommand<TRequest>( string methodName, Func<TRequest, IObservableCommand> implementation, Func<Exception, Exception> exceptionConverter, ByteSerializer<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject;

        /// <summary>
        /// Registers the given observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TResponse">The business type of the response</typeparam>
        /// <typeparam name="TResponseDto">The data transfer type of the response</typeparam>
        /// <param name="methodName">The local identifier of the command</param>
        /// <param name="implementation">The implementation of the command</param>
        /// <param name="exceptionConverter">A function that converts exceptions of the command</param>
        /// <param name="responseConverter">A function that converts the response</param>
        void RegisterObservableCommand<TRequest, TResponse, TResponseDto>( string methodName, Func<TRequest, IObservableCommand<TResponse>> implementation,
            Func<TResponse, TResponseDto> responseConverter, Func<Exception, Exception> exceptionConverter )
            where TRequest : class, ISilaRequestObject
            where TResponseDto : class;

        /// <summary>
        /// Registers the given observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TResponse">The business type of the response</typeparam>
        /// <typeparam name="TResponseDto">The data transfer type of the response</typeparam>
        /// <param name="methodName">The local identifier of the command</param>
        /// <param name="implementation">The implementation of the command</param>
        /// <param name="exceptionConverter">A function that converts exceptions of the command</param>
        /// <param name="responseConverter">A function that converts the response</param>
        /// <param name="requestSerializer">A serializer for the request data</param>
        /// <param name="responseSerializer">A serializer for the response data</param>
        void RegisterObservableCommand<TRequest, TResponse, TResponseDto>( string methodName, Func<TRequest, IObservableCommand<TResponse>> implementation,
            Func<TResponse, TResponseDto> responseConverter, Func<Exception, Exception> exceptionConverter, ByteSerializer<TRequest> requestSerializer, ByteSerializer<TResponseDto> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponseDto : class;

        /// <summary>
        /// Registers the given observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TIntermediate">The business type of the intermediate response</typeparam>
        /// <typeparam name="TIntermediateDto">The data transfer type of the intermediate response</typeparam>
        /// <param name="commandName">The local identifier of the command</param>
        /// <param name="implementation">The implementation of the command</param>
        /// <param name="exceptionConverter">A function that converts exceptions of the command</param>
        /// <param name="intermediateConverter">A function that converts intermediate responses</param>
        void RegisterCommandWithIntermediates<TRequest, TIntermediate, TIntermediateDto>( string commandName,
            Func<TRequest, IIntermediateObservableCommand<TIntermediate>> implementation, Func<TIntermediate, TIntermediateDto> intermediateConverter,
            Func<Exception, Exception> exceptionConverter )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class;

        /// <summary>
        /// Registers the given observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TIntermediate">The business type of the intermediate response</typeparam>
        /// <typeparam name="TIntermediateDto">The data transfer type of the intermediate response</typeparam>
        /// <param name="commandName">The local identifier of the command</param>
        /// <param name="implementation">The implementation of the command</param>
        /// <param name="exceptionConverter">A function that converts exceptions of the command</param>
        /// <param name="intermediateConverter">A function that converts intermediate responses</param>
        /// <param name="requestSerializer">A serializer for the request data</param>
        /// <param name="intermediatesSerializer">A serializer for the intermediate values</param>
        void RegisterCommandWithIntermediates<TRequest, TIntermediate, TIntermediateDto>( string commandName,
            Func<TRequest, IIntermediateObservableCommand<TIntermediate>> implementation, Func<TIntermediate, TIntermediateDto> intermediateConverter,
            Func<Exception, Exception> exceptionConverter, ByteSerializer<TRequest> requestSerializer, ByteSerializer<TIntermediateDto> intermediatesSerializer )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class;

        /// <summary>
        /// Registers the given observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TIntermediate">The business type of the intermediate response</typeparam>
        /// <typeparam name="TIntermediateDto">The data transfer type of the intermediate response</typeparam>
        /// <typeparam name="TResponse">The business type of the response</typeparam>
        /// <typeparam name="TResponseDto">The data transfer type of the response</typeparam>
        /// <param name="commandName">The local identifier of the command</param>
        /// <param name="implementation">The implementation of the command</param>
        /// <param name="exceptionConverter">A function that converts exceptions of the command</param>
        /// <param name="intermediateConverter">A function that converts intermediate responses</param>
        /// <param name="responseConverter">A function that converts the response</param>
        void RegisterCommandWithIntermediates<TRequest, TIntermediate, TIntermediateDto, TResponse, TResponseDto>( string commandName,
            Func<TRequest, IIntermediateObservableCommand<TIntermediate, TResponse>> implementation, Func<TIntermediate, TIntermediateDto> intermediateConverter,
            Func<TResponse, TResponseDto> responseConverter,
            Func<Exception, Exception> exceptionConverter )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class
            where TResponseDto : class;

        /// <summary>
        /// Registers the given observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TIntermediate">The business type of the intermediate response</typeparam>
        /// <typeparam name="TIntermediateDto">The data transfer type of the intermediate response</typeparam>
        /// <typeparam name="TResponse">The business type of the response</typeparam>
        /// <typeparam name="TResponseDto">The data transfer type of the response</typeparam>
        /// <param name="commandName">The local identifier of the command</param>
        /// <param name="implementation">The implementation of the command</param>
        /// <param name="exceptionConverter">A function that converts exceptions of the command</param>
        /// <param name="intermediateConverter">A function that converts intermediate responses</param>
        /// <param name="responseConverter">A function that converts the response</param>
        /// <param name="requestSerializer">A serializer for the request data</param>
        /// <param name="intermediatesSerializer">A serializer for the intermediate values</param>
        /// <param name="responseSerializer">A serializer for the response data</param>
        void RegisterCommandWithIntermediates<TRequest, TIntermediate, TIntermediateDto, TResponse, TResponseDto>( string commandName,
            Func<TRequest, IIntermediateObservableCommand<TIntermediate, TResponse>> implementation, Func<TIntermediate, TIntermediateDto> intermediateConverter,
            Func<TResponse, TResponseDto> responseConverter,
            Func<Exception, Exception> exceptionConverter,
            ByteSerializer<TRequest> requestSerializer,
            ByteSerializer<TIntermediateDto> intermediatesSerializer,
            ByteSerializer<TResponseDto> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class
            where TResponseDto : class;

        /// <summary>
        /// Registers that the server should allow to upload binaries for the provided command parameter
        /// </summary>
        /// <param name="commandParameter">The fully qualified identifier of the parameter</param>
        void RegisterBinaryCommandParameter( string commandParameter );
    }
}
