using System;
using Grpc.Core;
using Tecan.Sila2.Binary;

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

    /// <summary>
    /// Interface to describe call information
    /// </summary>
    public interface IClientCallInfo
    {
        /// <summary>
        /// The gRPC options for the call
        /// </summary>
        CallOptions Options { get; }

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
