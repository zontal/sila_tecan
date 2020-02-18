using System;
using System.Text;
using Grpc.Core;

namespace Tecan.Sila2
{
    /// <summary>
    /// Provides methods to generate SiLA errors and raise gRPC exceptions and to handle those exceptions.
    /// </summary>
    public class ErrorHandling
    {
        private const string CancellationIdentifier = "OperationCancelled";

        private static string ToBase64String(SiLAErrorDto error)
        {
            var bytes = ProtobufMarshaller<SiLAErrorDto>.Default.Serializer(error);
            return Convert.ToBase64String( bytes );
        }

        /// <summary>
        /// Creates a server-side validation error
        /// </summary>
        /// <param name="parameter">The fully qualified identifier of the erroneous parameter</param>
        /// <param name="message">The error message</param>
        /// <returns>An exception</returns>
        public static Exception CreateValidationError(string parameter, string message )
        {
            return new RpcException(new Status(StatusCode.Aborted, ToBase64String(new SiLAErrorDto
            {
                ValidationError = new ValidationErrorDto()
                {
                    Message = message,
                    Parameter = parameter
                }
            })));
        }

        /// <summary>
        /// Creates an exception for a non-classified server-side validation error
        /// </summary>
        /// <param name="exception">The argument exception</param>
        /// <returns>An exception</returns>
        public static Exception CreateUnknownValidationError( ArgumentException exception )
        {
            return new RpcException(new Status(StatusCode.Aborted, ToBase64String(new SiLAErrorDto
            {
                ValidationError = new ValidationErrorDto()
                {
                    Message = exception.Message,
                    Parameter = exception.ParamName
                }
            })));
        }

        /// <summary>
        /// Creates a server-side execution error
        /// </summary>
        /// <param name="identifier">The fully qualified identifier of the error</param>
        /// <param name="description">A description of the error</param>
        /// <param name="details">Error details</param>
        /// <returns>An exception</returns>
        public static Exception CreateExecutionError( string identifier, string description, string details = null )
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
        public static Exception CreateCancellationError( string cancellationMessage )
        {
            return CreateExecutionError( CancellationIdentifier, cancellationMessage );
        }

        /// <summary>
        /// Creates a server-side framework error
        /// </summary>
        /// <param name="identifier">The type of the error</param>
        public static Exception CreateFrameworkError(FrameworkErrorType identifier)
        {
            return new RpcException(new Status(StatusCode.Aborted, ToBase64String(new SiLAErrorDto
            {
                FrameworkError = new FrameworkErrorDto()
                {
                    Type = identifier
                }
            })));
        }

        /// <summary>
        /// Converts the given server exception
        /// </summary>
        /// <param name="ex">The exception from gRPC</param>
        /// <param name="executionErrorConversion">A function to convert defined errors</param>
        /// <returns>The converted exception</returns>
        public static Exception ConvertException(Exception ex,
            Func<DefinedExecutionErrorDto, Exception> executionErrorConversion = null)
        {
            RpcException rpcEx = ex is RpcException exception ? exception : ex.InnerException as RpcException;
            if (rpcEx != null)
            {
                if (rpcEx.Status.StatusCode == StatusCode.Aborted && !string.IsNullOrEmpty(rpcEx.Status.Detail))
                {
                    // check if SiLAError object is contained
                    try
                    {
                        var error = ProtobufMarshaller<SiLAErrorDto>.FromByteArray(
                            Convert.FromBase64String(rpcEx.Status.Detail));
                        if( error.DefinedExecutionError != null )
                        {
                            if( error.DefinedExecutionError.ErrorIdentifier == CancellationIdentifier )
                            {
                                return new OperationCanceledException( error.DefinedExecutionError.Message );
                            }
                            if( executionErrorConversion?.Invoke( error.DefinedExecutionError ) is var definedException && definedException != null )
                            {
                                return definedException;
                            }
                            else
                            {
                                return new SilaException($"Unknown Sila error {error.DefinedExecutionError.ErrorIdentifier} happened: {error.DefinedExecutionError.Message}");
                            }
                        }

                        if( error.UndefinedExecutionError != null )
                        {
                            return new SilaException(error.UndefinedExecutionError.Message);
                        }
                        if (error.FrameworkError != null)
                        {
                            return new SilaFrameworkException(error.FrameworkError.Type);
                        }
                        if (error.ValidationError != null)
                        {
                            if( error.ValidationError.Parameter != null )
                            {
                                var lastSlash = error.ValidationError.Parameter.LastIndexOf( '/' );
                                if( lastSlash != -1 && lastSlash < error.ValidationError.Parameter.Length - 1 )
                                {
                                    var parameterName = error.ValidationError.Parameter.Substring( lastSlash + 1 );
                                    parameterName = char.ToLowerInvariant( parameterName[0] ) + parameterName.Substring( 1 );
                                    return new ArgumentException( error.ValidationError.Message, parameterName );
                                }
                            }
                            return new ValidationException(error.ValidationError.Message, error.ValidationError.Parameter);
                        }
                        return new SilaException("An unknown error occured.");
                    }
                    catch (Exception)
                    {
                        // any other gRPC error => SiLA Connection Error
                        return new SilaException("SiLA Connection Error occured: " + rpcEx.Message);
                    }
                }
                else
                {
                    return new SilaException( "Non-SiLA gRPC exception occured", ex);
                }
            }

            return ex;
        }


    }
}