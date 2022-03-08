using System;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes an interface to create execution errors
    /// </summary>
    public interface IServerErrorHandling
    {
        /// <summary>
        /// Creates an exception that a metadata is missing
        /// </summary>
        /// <param name="metadataIdentifier">The fully qualified metadata identifier</param>
        /// <returns>An exception</returns>
        Exception CreateMissingMetadataException( string metadataIdentifier );

        /// <summary>
        /// Creates a server-side validation error
        /// </summary>
        /// <param name="parameter">The fully qualified identifier of the erroneous parameter</param>
        /// <param name="message">The error message</param>
        /// <returns>An exception</returns>
        Exception CreateValidationError( string parameter, string message );

        /// <summary>
        /// Creates an exception for a non-classified server-side validation error
        /// </summary>
        /// <param name="exception">The argument exception</param>
        /// <returns>An exception</returns>
        Exception CreateUnknownValidationError( ArgumentException exception );

        /// <summary>
        /// Creates an exception for a server-side undefined execution error
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <returns>An exception</returns>
        Exception CreateUndefinedExecutionError( Exception exception );

        /// <summary>
        /// Creates a server-side execution error
        /// </summary>
        /// <param name="identifier">The fully qualified identifier of the error</param>
        /// <param name="description">A description of the error</param>
        /// <param name="details">Error details</param>
        /// <returns>An exception</returns>
        Exception CreateExecutionError( string identifier, string description, string details = null );

        /// <summary>
        /// Creates a server-side exception for a cancellation error
        /// </summary>
        /// <param name="cancellationMessage">The message explaining how and why the operation was cancelled</param>
        /// <returns>An exception</returns>
        Exception CreateCancellationError( string cancellationMessage );
    }
}
