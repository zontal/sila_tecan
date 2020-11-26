using System;

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
        /// Registers the given observable property
        /// </summary>
        /// <typeparam name="T">The data transfer type of the property</typeparam>
        /// <param name="propertyName">The local identifier of the property</param>
        /// <param name="implementation">The implementation of the property, i.e. the getter</param>
        void RegisterObservableProperty<T>( string propertyName, Func<T> implementation ) where T : class;

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
    }
}
