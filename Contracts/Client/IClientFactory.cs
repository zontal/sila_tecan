using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Denotes a factory to create strongly typed clients for well-known features
    /// </summary>
    public interface IClientFactory
    {
        /// <summary>
        /// Gets the fully-qualified identifier of the feature for which clients can be generated
        /// </summary>
        public string FeatureIdentifier { get; }

        /// <summary>
        /// Gets the interface type for which clients can be generated
        /// </summary>
        Type InterfaceType { get; }

        /// <summary>
        /// Creates a strongly typed client for the given execution channel and execution manager
        /// </summary>
        /// <param name="channel">The channel that should be used for communication with the server</param>
        /// <param name="executionManager">The execution manager to manage metadata</param>
        /// <returns>A strongly typed client. This object will be an instance of the InterfaceType property</returns>
        object CreateClient( IClientChannel channel, IClientExecutionManager executionManager );
    }
}
