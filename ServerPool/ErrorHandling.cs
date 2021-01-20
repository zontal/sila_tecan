using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.ServerPooling
{
    internal static class ErrorHandling
    {
        private const string CancellationIdentifier = "OperationCancelled";

        public static Exception ConvertException(SiLAErrorDto error, Func<string, string, Exception> executionErrorConversion )
        {
            if(error.DefinedExecutionError != null)
            {
                if(error.DefinedExecutionError.ErrorIdentifier == CancellationIdentifier)
                {
                    return new OperationCanceledException( error.DefinedExecutionError.Message );
                }
                if(executionErrorConversion?.Invoke( error.DefinedExecutionError.ErrorIdentifier, error.DefinedExecutionError.Message ) is var definedException && definedException != null)
                {
                    return definedException;
                }
                else
                {
                    return new DefinedErrorException( error.DefinedExecutionError.ErrorIdentifier, error.DefinedExecutionError.Message );
                }
            }

            if(error.UndefinedExecutionError != null)
            {
                return new SilaException( error.UndefinedExecutionError.Message );
            }
            if(error.FrameworkError != null)
            {
                return new SilaFrameworkException( error.FrameworkError.Type );
            }
            if(error.ValidationError != null)
            {
                if(error.ValidationError.Parameter != null)
                {
                    var lastSlash = error.ValidationError.Parameter.LastIndexOf( '/' );
                    if(lastSlash != -1 && lastSlash < error.ValidationError.Parameter.Length - 1)
                    {
                        var parameterName = error.ValidationError.Parameter.Substring( lastSlash + 1 );
                        parameterName = char.ToLowerInvariant( parameterName[0] ) + parameterName.Substring( 1 );
                        return new ArgumentException( error.ValidationError.Message, parameterName );
                    }
                }
                return new ValidationException( error.ValidationError.Message, error.ValidationError.Parameter );
            }
            return new SilaException( "An unknown error occured." );
        }

        internal static Exception ConvertException( BinaryTransferError binaryError, string binaryStorageId )
        {
            switch(binaryError.Type)
            {
                case BinaryTransferError.ErrorType.InvalidBinaryTransferUuid:
                    return new InvalidBinaryTransferIdException( binaryError.Message, binaryStorageId );
                case BinaryTransferError.ErrorType.BinaryUploadFailed:
                    return new BinaryUploadFailedException( binaryError.Message, binaryStorageId );
                case BinaryTransferError.ErrorType.BinaryDownloadFailed:
                    return new BinaryDownloadFailedException( binaryError.Message, binaryStorageId );
                default:
                    return new BinaryTransferException( binaryError.Message, binaryStorageId );
            }
        }

        public static Exception ConvertException(Exception exception, Func<string, string, Exception> innerConverter)
        {
            if (exception is TemporaryException temp)
            {
                return ConvertException( temp.Error, innerConverter );
            }
            else if (exception is AggregateException aggregateException)
            {
                var tempException = aggregateException.InnerExceptions.OfType<TemporaryException>().FirstOrDefault();
                if (tempException != null)
                {
                    return ConvertException( tempException.Error, innerConverter );
                }
                if (aggregateException.InnerException is TaskCanceledException)
                {
                    return new OperationCanceledException();
                }
                if (aggregateException.InnerExceptions.Count == 1)
                {
                    return aggregateException.InnerException;
                }
                return aggregateException;
            }
            else
            {
                return exception;
            }
        }
    }
}
