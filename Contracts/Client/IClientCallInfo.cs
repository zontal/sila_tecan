using System;
using System.Collections.Generic;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Interface to describe call information
    /// </summary>
    public interface IClientCallInfo
    {
        /// <summary>
        /// The gRPC options for the call
        /// </summary>
        IDictionary<string, byte[]> Metadata { get; }

        /// <summary>
        /// Notifies that the call has finished successfully
        /// </summary>
        void FinishSuccessful();

        /// <summary>
        /// Notifies that the call finished with the given exception
        /// </summary>
        /// <param name="exception"></param>
        void FinishWithErrors(Exception exception);
    }
}
