using System.Collections.Generic;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Denotes a component that intercepts calls on the client side
    /// </summary>
    public interface IClientRequestInterceptor
    {
        /// <summary>
        /// The fully qualified identifier for the metadata that is intercepted or null, if all requests should be intercepted
        /// </summary>
        string MetadataIdentifier { get; }

        /// <summary>
        /// Intercepts a call to the given server with the given metadata, filling metadata for a given call
        /// </summary>
        /// <param name="server">The server that should be connected</param>
        /// <param name="executionManager">The execution manager that handles the calls to this server</param>
        /// <param name="metadata">The metadata that should be filled</param>
        /// <returns></returns>
        IClientRequestInterception Intercept( ServerData server, IClientExecutionManager executionManager, IDictionary<string, byte[]> metadata );
    }
}
