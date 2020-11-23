using System;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Denotes an interception of a call
    /// </summary>
    public interface IClientRequestInterception
    {
        /// <summary>
        /// Reports that the call has been completed successfully
        /// </summary>
        void CompleteSuccessfully();

        /// <summary>
        /// Reports that the call completed with an error
        /// </summary>
        /// <param name="exception">The error that has happened</param>
        void CompleteWithError( Exception exception );
    }
}