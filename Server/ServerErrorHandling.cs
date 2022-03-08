using System;
using Grpc.Core;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Provides methods to generate SiLA errors and raise gRPC exceptions and to handle those exceptions.
    /// </summary>
    public class ServerErrorHandling : IServerErrorHandling
    {
        /// <summary>
        /// The server error handling instance
        /// </summary>
        public static readonly ServerErrorHandling Instance = new ServerErrorHandling();

        private const string CancellationIdentifier = "OperationCancelled";

        private static string ToBase64String( SiLAErrorDto error )
        {
            var bytes = ProtobufMarshaller<SiLAErrorDto>.Default.Serializer( error );
            return Convert.ToBase64String( bytes );
        }

        private static string ToBase64String( BinaryTransferError error )
        {
            var bytes = ProtobufMarshaller<BinaryTransferError>.Default.Serializer( error );
            return Convert.ToBase64String( bytes );
        }

        /// <summary>
        /// Creates a server-side validation error
        /// </summary>
        /// <param name="parameter">The fully qualified identifier of the erroneous parameter</param>
        /// <param name="message">The error message</param>
        /// <returns>An exception</returns>
        public Exception CreateValidationError( string parameter, string message )
        {
            return new RpcException( new Status( StatusCode.Aborted, ToBase64String( new SiLAErrorDto
            {
                ValidationError = new ValidationErrorDto()
                {
                    Message = message,
                    Parameter = parameter
                }
            } ) ) );
        }

        /// <summary>
        /// Creates an exception for a non-classified server-side validation error
        /// </summary>
        /// <param name="exception">The argument exception</param>
        /// <returns>An exception</returns>
        public Exception CreateUnknownValidationError( ArgumentException exception )
        {
            return new RpcException( new Status( StatusCode.Aborted, ToBase64String( new SiLAErrorDto
            {
                ValidationError = new ValidationErrorDto()
                {
                    Message = exception.Message,
                    Parameter = exception.ParamName
                }
            } ) ) );
        }

        /// <summary>
        /// Creates an exception for a server-side undefined execution error
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <returns>An exception</returns>
        public Exception CreateUndefinedExecutionError( Exception exception )
        {
            return new RpcException( new Status( StatusCode.Aborted, ToBase64String( new SiLAErrorDto
            {
                UndefinedExecutionError = new UndefinedExecutionErrorDto
                {
                    Message = $"{exception.GetType().Name}: {exception.Message}"
                }
            } ) ) );
        }

        /// <summary>
        /// Create an exception to represent a binary transfer error
        /// </summary>
        /// <param name="binaryTransferError">The binary transfer error</param>
        /// <returns>The exception to raise</returns>
        public Exception CreateBinaryError( BinaryTransferError binaryTransferError )
        {
            return new RpcException( new Status( StatusCode.Aborted, ToBase64String( binaryTransferError ) ) );
        }

        /// <summary>
        /// Creates a server-side execution error
        /// </summary>
        /// <param name="identifier">The fully qualified identifier of the error</param>
        /// <param name="description">A description of the error</param>
        /// <param name="details">Error details</param>
        /// <returns>An exception</returns>
        public Exception CreateExecutionError( string identifier, string description, string details = null )
        {
            return new RpcException( new Status( StatusCode.Aborted, ToBase64String( new SiLAErrorDto
            {
                DefinedExecutionError = new DefinedExecutionErrorDto()
                {
                    Message = details != null ? $"{description}: {details}" : description,
                    ErrorIdentifier = identifier
                }
            } ) ) );
        }

        /// <summary>
        /// Creates a server-side exception for a cancellation error
        /// </summary>
        /// <param name="cancellationMessage">The message explaining how and why the operation was cancelled</param>
        /// <returns>An exception</returns>
        public Exception CreateCancellationError( string cancellationMessage )
        {
            return CreateExecutionError( CancellationIdentifier, cancellationMessage );
        }

        /// <summary>
        /// Creates a server-side framework error
        /// </summary>
        /// <param name="identifier">The type of the error</param>
        public static Exception CreateFrameworkError( FrameworkErrorType identifier )
        {
            return new RpcException( new Status( StatusCode.Aborted, ToBase64String( new SiLAErrorDto
            {
                FrameworkError = new FrameworkErrorDto()
                {
                    Type = identifier
                }
            } ) ) );
        }

        /// <inheritdoc />
        public Exception CreateMissingMetadataException( string metadataIdentifier )
        {
            return CreateFrameworkError( FrameworkErrorType.InvalidMetadata );
        }
    }
}