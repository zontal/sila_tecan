using System;
using System.Text;
using Grpc.Core;

namespace Tecan.Sila2
{
    /// <summary>
    /// Provides methods to generate SiLA errors and raise gRPC exceptions and to handle those execptions.
    /// </summary>
    public class ErrorHandling
    {
        private static string ToUtf8String(SiLAErrorDto error)
        {
            var bytes = ProtobufMarshaller<SiLAErrorDto>.Default.Serializer(error);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Creates a server-side validation error
        /// </summary>
        /// <param name="parameter">The erroneous parameter</param>
        /// <param name="value">The actually provided value</param>
        /// <param name="details">Details of the error</param>
        /// <returns>An exception</returns>
        public static Exception RaiseValidationError(string parameter, string value, string details = null)
        {
            return new RpcException(new Status(StatusCode.Aborted, ToUtf8String(new SiLAErrorDto
            {
                ValidationError = new ValidationErrorDto()
                {
                    Message = $"Provided value {value} is invalid, {details}",
                    Parameter = parameter
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
            return new RpcException( new Status( StatusCode.Aborted, ToUtf8String( new SiLAErrorDto
            {
                DefinedExecutionError = new DefinedExecutionErrorDto()
                {
                    Message = details ?? description,
                    ErrorIdentifier = identifier
                }
            } ) ) );
        }

        /// <summary>
        /// Creates a server-side framework error
        /// </summary>
        /// <param name="identifier">The type of the error</param>
        public static Exception CreateFrameworkError(FrameworkErrorType identifier)
        {
            return new RpcException(new Status(StatusCode.Aborted, ToUtf8String(new SiLAErrorDto
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
                            Encoding.UTF8.GetBytes(rpcEx.Status.Detail));
                        if( error.DefinedExecutionError != null )
                        {
                            if( executionErrorConversion?.Invoke( error.DefinedExecutionError ) is Exception definedException )
                            {
                                return definedException;
                            }
                            else
                            {
                                throw new SilaException($"Unknown Sila error {error.DefinedExecutionError.ErrorIdentifier} happened: {error.DefinedExecutionError.Message}");
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