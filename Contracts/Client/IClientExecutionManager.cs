using System;
using System.Collections.Generic;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Interface for a component to manage client connections
    /// </summary>
    public interface IClientExecutionManager
    {
        /// <summary>
        /// Creates the call information for the given command
        /// </summary>
        /// <param name="commandIdentifier">The fully qualified identifier of the property or command that is requested</param>
        /// <returns>Call information, including metadata</returns>
        IClientCallInfo CreateCallOptions(string commandIdentifier);

        /// <summary>
        /// Gets the binary store used for downloading binary data
        /// </summary>
        IBinaryStore DownloadBinaryStore
        {
            get;
        }

        /// <summary>
        /// Creates a binary store for the given command parameter
        /// </summary>
        /// <param name="commandParameterIdentifier">The fully qualified command parameter identifier</param>
        /// <returns>A binary store used to upload binary data to the server</returns>
        IBinaryStore CreateBinaryStore( string commandParameterIdentifier );
    }
}
