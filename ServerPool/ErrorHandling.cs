using System;
using System.Collections.Generic;
using System.Text;

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

        public static Exception ConvertException(Exception exception, Func<string, string, Exception> innerConverter)
        {
            if (exception is TemporaryException temp)
            {
                return ConvertException( temp.Error, innerConverter );
            }
            else
            {
                return exception;
            }
        }
    }
}
