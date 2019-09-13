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

        public static void RaiseValidationError(string parameter, string value, string details = null)
        {
            throw new RpcException(new Status(StatusCode.Aborted, ToUtf8String(new SiLAErrorDto
            {
                ValidationError = new ValidationErrorDto()
                {
                    Message = $"Provided value {value} is invalid, {details}",
                    Parameter = parameter
                }
            })));
        }

        public static Exception CreateExecutionError(string identifier, string description, string details = null)
        {
            return new RpcException(new Status(StatusCode.Aborted, ToUtf8String(GetExecutionError(identifier, description, details))));
        }

        public static SiLAErrorDto GetExecutionError(string identifier, string description, string details = null)
        {
            return new SiLAErrorDto
            {
                DefinedExecutionError = new DefinedExecutionErrorDto()
                {
                    Message = details ?? description,
                    ErrorIdentifier = identifier
                }
            };
        }

        public static void RaiseFrameworkError(FrameworkErrorType identifier)
        {
            throw new RpcException(new Status(StatusCode.Aborted, ToUtf8String(new SiLAErrorDto
            {
                FrameworkError = new FrameworkErrorDto()
                {
                    Type = identifier
                }
            })));
        }

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
                                throw new Exception($"Unknown Sila error {error.DefinedExecutionError.ErrorIdentifier} happened: {error.DefinedExecutionError.Message}");
                            }
                        }

                        if (error.FrameworkError != null)
                        {
                            return new Exception($"Error in Sila Framework: {GetErrorMessage(error.FrameworkError.Type)} ({error.FrameworkError.Type})");
                        }
                        if (error.ValidationError != null)
                        {
                            return new Exception($"The provided input for {error.ValidationError.Parameter} failed to validate: {error.ValidationError.Message}");
                        }
                        return new Exception("An unknown error occured.");
                    }
                    catch (Exception)
                    {
                        // any other gRPC error => SiLA Connection Error
                        return new Exception("SiLA Connection Error occured: " + rpcEx.Message);
                    }
                }
                else
                {
                    return new InvalidOperationException("Non-SiLA gRPC exception occured", ex);
                }
            }

            return ex;
        }


        private static string GetErrorMessage(FrameworkErrorType errorType)
        {
            switch (errorType)
            {
                case FrameworkErrorType.CommandExecutionNotAccepted:
                    return "The server refused to execute the command.";
                case FrameworkErrorType.CommandExecutionNotFinished:
                    return "The command is not finished, yet.";
                case FrameworkErrorType.InvalidCommandExecutionUuid:
                    return "The provided command identifier does not correspond to a command.";
                default:
                    return "An unknown error occurred.";
            }
        }
    }
}