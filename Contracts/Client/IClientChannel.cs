using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.Client
{
    public interface IClientChannel
    {
        T ReadProperty<T>( string serviceName, string propertyName, IClientCallInfo callInfo ) where T : class, ISilaTransferObject;
        T ReadProperty<T>( string serviceName, string propertyName, IClientCallInfo callInfo, ByteSerializer<T> serializer ) where T : class, ISilaTransferObject;
        Task<T> ReadPropertyAsync<T>( string serviceName, string propertyName, IClientCallInfo callInfo, ByteSerializer<T> serializer ) where T : class, ISilaTransferObject;

        Task SubscribeProperty<T>( string serviceName, string propertyName, Action<T> newValueReceived, IClientCallInfo callInfo, CancellationToken cancellationToken = default ) where T : class, ISilaTransferObject;

        Task SubscribeProperty<T>( string serviceName, string propertyName, Action<T> newValueReceived, IClientCallInfo callInfo, ByteSerializer<T> serializer, CancellationToken cancellationToken = default ) where T : class, ISilaTransferObject;

        void ExecuteUnobservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject;

        void ExecuteUnobservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject;

        Task ExecuteUnobservableCommandAsync<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject;

        TResponse ExecuteUnobservableCommand<TRequest, TResponse>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
            where TResponse : class;

        TResponse ExecuteUnobservableCommand<TRequest, TResponse>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer,
            ByteSerializer<TResponse> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponse : class;

        Task<TResponse> ExecuteUnobservableCommandAsync<TRequest, TResponse>(
            string serviceName,
            string commandName,
            TRequest request,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer,
            ByteSerializer<TResponse> responseSerializer )
            where TRequest : class, ISilaRequestObject
            where TResponse : class;

        IObservableCommand ExecuteObservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject;

        IObservableCommand ExecuteObservableCommand<TRequest>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo,
            ByteSerializer<TRequest> requestSerializer )
            where TRequest : class, ISilaRequestObject;

        IObservableCommand<TResponse> ExecuteObservableCommand<TRequest, TResponse, TResponseDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TResponseDto, TResponse> responseConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
            where TResponseDto : class, ISilaTransferObject;

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

        IIntermediateObservableCommand<TIntermediate> ExecuteIntermediatesCommand<TRequest, TIntermediate, TIntermediateDto>(
            string serviceName,
            string commandName,
            TRequest request,
            Func<TIntermediateDto, TIntermediate> intermediateConverter,
            Func<string, string, Exception> errorConverter,
            IClientCallInfo callInfo )
            where TRequest : class, ISilaRequestObject
            where TIntermediateDto : class, ISilaTransferObject;

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

        IEnumerable<string> GetAffected( string serviceName, string metadataIdentifier );

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

    public struct ByteSerializer<T>
    {
        public Func<T, byte[]> Serializer
        {
            get;
        }

        public Func<byte[], T> Deserializer
        {
            get;
        }

        public ByteSerializer(Func<T, byte[]> serializer, Func<byte[], T> deserializer)
        {
            Serializer = serializer;
            Deserializer = deserializer;
        }
    }
}
