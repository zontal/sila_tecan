using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Denotes a component that can execute SiLA2 commands
    /// </summary>
    public interface IClientChannel
    {
        /// <summary>
        /// Reads a property through the channel
        /// </summary>
        /// <typeparam name="T">The data transfer object type of the property response</typeparam>
        /// <param name="serviceName">The service name for the property</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <returns>The property value as data transfer object</returns>
        T ReadProperty<T>( string serviceName, string propertyName, IClientCallInfo callInfo ) where T : class, ISilaTransferObject;

        /// <summary>
        /// Reads a property through the channel
        /// </summary>
        /// <typeparam name="T">The data transfer object type of the property response</typeparam>
        /// <param name="serviceName">The service name for the property</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <param name="serializer">A custom serializer for the property response</param>
        /// <returns>The property value as data transfer object</returns>
        T ReadProperty<T>( string serviceName, string propertyName, IClientCallInfo callInfo, ByteSerializer<T> serializer ) where T : class, ISilaTransferObject;

        /// <summary>
        /// Reads a property through the channel asynchronously
        /// </summary>
        /// <typeparam name="T">The data transfer object type of the property response</typeparam>
        /// <param name="serviceName">The service name for the property</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <param name="serializer">A custom serializer for the property response</param>
        /// <returns>A task that returns the property value as data transfer object</returns>
        Task<T> ReadPropertyAsync<T>( string serviceName, string propertyName, IClientCallInfo callInfo, ByteSerializer<T> serializer ) where T : class, ISilaTransferObject;

        /// <summary>
        /// Subscribes to the given property
        /// </summary>
        /// <typeparam name="T">The data transfer object type of the property response</typeparam>
        /// <param name="serviceName">The service name for the property</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="newValueReceived">A callback that should be called when a new value is received</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the subscription</param>
        /// <returns>A task that ends when the subscription is terminated</returns>
        Task SubscribeProperty<T>( string serviceName, string propertyName, Action<T> newValueReceived, IClientCallInfo callInfo, CancellationToken cancellationToken = default ) where T : class, ISilaTransferObject;

        /// <summary>
        /// Subscribes to the given property
        /// </summary>
        /// <typeparam name="T">The data transfer object type of the property response</typeparam>
        /// <param name="serviceName">The service name for the property</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="newValueReceived">A callback that should be called when a new value is received</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <param name="serializer">A custom serializer for the property response</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the subscription</param>
        /// <returns>A task that ends when the subscription is terminated</returns>
        Task SubscribeProperty<T>( string serviceName, string propertyName, Action<T> newValueReceived, IClientCallInfo callInfo, ByteSerializer<T> serializer, CancellationToken cancellationToken = default ) where T : class, ISilaTransferObject;

        /// <summary>
        /// Executes an unobservable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        void ExecuteUnobservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject;

        /// <summary>
        /// Executes an unobservable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <param name="requestSerializer">A custom serializer for the request</param>
        void ExecuteUnobservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject;

        /// <summary>
        /// Executes an unobservable command asynchronously
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <param name="requestSerializer">A custom serializer for the request</param>
        Task ExecuteUnobservableCommandAsync<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject;

        /// <summary>
        /// Executes an unobservable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TResponse">The data transfer type of the response</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <returns>A data transfer object of the command response</returns>
        TResponse ExecuteUnobservableCommand<TRequest, TResponse>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
            where TResponse : class;

        /// <summary>
        /// Executes an unobservable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TResponse">The data transfer type of the response</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <param name="requestSerializer">A custom serializer for the request</param>
        /// <param name="responseSerializer">A custom serializer for the response</param>
        /// <returns>A data transfer object of the command response</returns>
        TResponse ExecuteUnobservableCommand<TRequest, TResponse>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer,
            ByteSerializer<TResponse> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponse : class;

        /// <summary>
        /// Executes an unobservable command asynchronously
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TResponse">The data transfer type of the response</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <param name="requestSerializer">A custom serializer for the request</param>
        /// <param name="responseSerializer">A custom serializer for the response</param>
        /// <returns>A task that returns the data transfer object of the command response</returns>
        Task<TResponse> ExecuteUnobservableCommandAsync<TRequest, TResponse>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer,
            ByteSerializer<TResponse> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponse : class;

        /// <summary>
        /// Executes an observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="errorConverter">A callback method to translate SiLA2 error codes</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <returns>An object representing the observable command execution</returns>
        IObservableCommand ExecuteObservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject;

        /// <summary>
        /// Executes an observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="errorConverter">A callback method to translate SiLA2 error codes</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <param name="requestSerializer">A custom serializer for the request</param>
        /// <returns>An object representing the observable command execution</returns>
        IObservableCommand ExecuteObservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject;

        /// <summary>
        /// Executes an observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TResponse">The business type of the response</typeparam>
        /// <typeparam name="TResponseDto">The data transfer type of the response</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="responseConverter">A callback method to extract the business response object from the data transfer object</param>
        /// <param name="errorConverter">A callback method to translate SiLA2 error codes</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <returns>An object representing the observable command execution</returns>
        IObservableCommand<TResponse> ExecuteObservableCommand<TRequest, TResponse, TResponseDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TResponseDto, TResponse> responseConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
            where TResponseDto : class, ISilaTransferObject;

        /// <summary>
        /// Executes an observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TResponse">The business type of the response</typeparam>
        /// <typeparam name="TResponseDto">The data transfer type of the response</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="responseConverter">A callback method to extract the business response object from the data transfer object</param>
        /// <param name="errorConverter">A callback method to translate SiLA2 error codes</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <param name="requestSerializer">A custom serializer for the request</param>
        /// <param name="responseSerializer">A custom serializer for the response</param>
        /// <returns>An object representing the observable command execution</returns>
        IObservableCommand<TResponse> ExecuteObservableCommand<TRequest, TResponse, TResponseDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TResponseDto, TResponse> responseConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer,
            ByteSerializer<TResponseDto> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponseDto : class, ISilaTransferObject;

        /// <summary>
        /// Executes an observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TIntermediate">The business type of the intermediate response</typeparam>
        /// <typeparam name="TIntermediateDto">The data transfer type of the intermediate response</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="intermediateConverter">A callback method to extract the business intermediate result from the data transfer object</param>
        /// <param name="errorConverter">A callback method to translate SiLA2 error codes</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <returns>An object representing the observable command execution</returns>
        IIntermediateObservableCommand<TIntermediate> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TIntermediateDto, TIntermediate> intermediateConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class, ISilaTransferObject;

        /// <summary>
        /// Executes an observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TIntermediate">The business type of the intermediate response</typeparam>
        /// <typeparam name="TIntermediateDto">The data transfer type of the intermediate response</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="intermediateConverter">A callback method to extract the business intermediate result from the data transfer object</param>
        /// <param name="errorConverter">A callback method to translate SiLA2 error codes</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <param name="requestSerializer">A custom serializer for the request</param>
        /// <param name="intermediateSerializer"></param>
        /// <returns>An object representing the observable command execution</returns>
        IIntermediateObservableCommand<TIntermediate> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TIntermediateDto, TIntermediate> intermediateConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer,
            ByteSerializer<TIntermediateDto> intermediateSerializer )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class, ISilaTransferObject;

        /// <summary>
        /// Executes an observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TIntermediate">The business type of the intermediate response</typeparam>
        /// <typeparam name="TIntermediateDto">The data transfer type of the intermediate response</typeparam>
        /// <typeparam name="TResponse">The business type of the response</typeparam>
        /// <typeparam name="TResponseDto">The data transfer type of the response</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="intermediateConverter">A callback method to extract the business intermediate result from the data transfer object</param>
        /// <param name="responseConverter">A callback method to extract the business response object from the data transfer object</param>
        /// <param name="errorConverter">A callback method to translate SiLA2 error codes</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <returns>An object representing the observable command execution</returns>
        IIntermediateObservableCommand<TIntermediate, TResponse> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto, TResponse, TResponseDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TIntermediateDto, TIntermediate> intermediateConverter,
            Func<TResponseDto, TResponse> responseConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class, ISilaTransferObject
            where TResponseDto : class, ISilaTransferObject;

        /// <summary>
        /// Executes an observable command
        /// </summary>
        /// <typeparam name="TRequest">The data transfer type of the request</typeparam>
        /// <typeparam name="TIntermediate">The business type of the intermediate response</typeparam>
        /// <typeparam name="TIntermediateDto">The data transfer type of the intermediate response</typeparam>
        /// <typeparam name="TResponse">The business type of the response</typeparam>
        /// <typeparam name="TResponseDto">The data transfer type of the response</typeparam>
        /// <param name="serviceName">The service name of the command</param>
        /// <param name="commandName">The name of the command</param>
        /// <param name="request">The request object</param>
        /// <param name="intermediateConverter">A callback method to extract the business intermediate result from the data transfer object</param>
        /// <param name="responseConverter">A callback method to extract the business response object from the data transfer object</param>
        /// <param name="errorConverter">A callback method to translate SiLA2 error codes</param>
        /// <param name="callInfo">An object that encapsulates the call information</param>
        /// <param name="requestSerializer">A custom serializer for the request</param>
        /// <param name="intermediateSerializer"></param>
        /// <param name="responseSerializer">A custom serializer for the response</param>
        /// <returns>An object representing the observable command execution</returns>
        IIntermediateObservableCommand<TIntermediate, TResponse> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto, TResponse, TResponseDto>(
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
            where TResponseDto : class, ISilaTransferObject;

        /// <summary>
        /// Gets the affected feature or command or property identifiers for the given metadata
        /// </summary>
        /// <param name="serviceName">service name of the metadata</param>
        /// <param name="metadataIdentifier">The unique identifier of the metadata</param>
        /// <returns>A collection of unique identifiers of features, properties or commands</returns>
        IEnumerable<string> GetAffected( string serviceName, string metadataIdentifier );

        /// <summary>
        /// Creates a binary store for the given command parameter
        /// </summary>
        /// <param name="commandParameterIdentifier">A unique identifier of a command parameter or null, in which case the binary store can only be used for download.</param>
        /// <param name="executionManager">A component that manages executions</param>
        /// <returns>A component that can be used for transfer of binaries from and to the server</returns>
        IBinaryStore CreateBinaryStore( string commandParameterIdentifier, IClientExecutionManager executionManager );
        
        /// <summary>
        /// Converts the given server exception
        /// </summary>
        /// <param name="ex">The exception from gRPC</param>
        /// <param name="executionErrorConversion">A function to convert defined errors</param>
        /// <returns>The converted exception</returns>
        Exception ConvertException( Exception ex,
            Func<string, string, Exception> executionErrorConversion = null );
    }
}
