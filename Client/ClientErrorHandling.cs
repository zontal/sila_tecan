using Grpc.Core;
using System;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.Client
{
    internal static class ClientErrorHandling
    {

        private const string CancellationIdentifier = "OperationCancelled";

        public static Exception ConvertException( Exception ex,
            Func<string, string, Exception> executionErrorConversion = null )
        {
            RpcException rpcEx = ex is RpcException exception ? exception : ex.InnerException as RpcException;
            if(rpcEx != null)
            {
                if(rpcEx.Status.StatusCode == StatusCode.Aborted && !string.IsNullOrEmpty( rpcEx.Status.Detail ))
                {
                    // check if SiLAError object is contained
                    try
                    {
                        return ConvertExceptionCore(executionErrorConversion, rpcEx);
                    }
                    catch (Exception conversionException)
                    {
                        // any other gRPC error => SiLA Connection Error
                        return new SilaException( "SiLA error could not be translated: " + conversionException.Message, conversionException );
                    }
                }
                else if(rpcEx.StatusCode == StatusCode.Cancelled)
                {
                    return new OperationCanceledException( "The operation was cancelled at the server", rpcEx );
                }
                else
                {
                    return new SilaException( "Non-SiLA gRPC exception occured", ex );
                }
            }

            return ex;
        }

        private static Exception ConvertExceptionCore(Func<string, string, Exception> executionErrorConversion, RpcException rpcEx)
        {
            var error = ByteSerializer.FromByteArray<SiLAErrorDto>(
                                        Convert.FromBase64String(rpcEx.Status.Detail), FrameworkTypeModel.Instance);
            if (error.DefinedExecutionError != null)
            {
                if (error.DefinedExecutionError.ErrorIdentifier == CancellationIdentifier)
                {
                    return new OperationCanceledException(error.DefinedExecutionError.Message);
                }
                if (executionErrorConversion?.Invoke(error.DefinedExecutionError.ErrorIdentifier, error.DefinedExecutionError.Message) is var definedException && definedException != null)
                {
                    return definedException;
                }
                else
                {
                    return new DefinedErrorException(error.DefinedExecutionError.ErrorIdentifier, error.DefinedExecutionError.Message);
                }
            }

            if (error.UndefinedExecutionError != null)
            {
                return new SilaException(error.UndefinedExecutionError.Message);
            }
            if (error.FrameworkError != null)
            {
                return new SilaFrameworkException(error.FrameworkError.Type);
            }
            if (error.ValidationError != null)
            {
                if (error.ValidationError.Parameter != null)
                {
                    var lastSlash = error.ValidationError.Parameter.LastIndexOf('/');
                    if (lastSlash != -1 && lastSlash < error.ValidationError.Parameter.Length - 1)
                    {
                        var parameterName = error.ValidationError.Parameter.Substring(lastSlash + 1);
                        parameterName = char.ToLowerInvariant(parameterName[0]) + parameterName.Substring(1);
                        return new ArgumentException(error.ValidationError.Message, parameterName);
                    }
                }
                return new ValidationException(error.ValidationError.Message, error.ValidationError.Parameter);
            }
            return new SilaException("An unknown error occured.");
        }

        public static Exception ConvertBinaryException( RpcException rpcEx, string binaryIdentifier )
        {
            try
            {
                var binaryTransferError = ByteSerializer.FromByteArray<BinaryTransferError>( Convert.FromBase64String( rpcEx.Status.Detail ), FrameworkTypeModel.Instance );
                switch(binaryTransferError.Type)
                {
                    case BinaryTransferError.ErrorType.InvalidBinaryTransferUuid:
                        return new InvalidBinaryTransferIdException( binaryTransferError.Message, binaryIdentifier );
                    case BinaryTransferError.ErrorType.BinaryUploadFailed:
                        return new BinaryUploadFailedException( binaryTransferError.Message, binaryIdentifier );
                    case BinaryTransferError.ErrorType.BinaryDownloadFailed:
                        return new BinaryDownloadFailedException( binaryTransferError.Message, binaryIdentifier );
                    default:
                        return new BinaryTransferException( binaryTransferError.Message, binaryIdentifier );
                }
            }
            catch (Exception exception)
            {
                return new BinaryTransferException( exception.Message, binaryIdentifier, rpcEx );
            }
        }
    }
}
