using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes an interface to build a server configuration for a given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IServiceConfigurationBuilder<T> where T : class
    {
        /// <summary>
        /// Adds a service handler for the given type
        /// </summary>
        /// <param name="serviceHandler">The service handler</param>
        void AddServiceHandler( IServiceHandler<T> serviceHandler );

        /// <summary>
        /// Gets the registered service handlers
        /// </summary>
        IEnumerable<IServiceHandler<T>> ServiceHandlers { get; }

        /// <summary>
        /// Configures the server for the given server unique identifier
        /// </summary>
        /// <param name="guid">The unique identifier of the server</param>
        /// <param name="port">The port on which the server should be running</param>
        /// <param name="networkInterfaceFilter"></param>
        [Obsolete]
        void ConfigureForServer( Guid guid, int port, Predicate<NetworkInterface> networkInterfaceFilter );

        /// <summary>
        /// Configures the server to accept connections from the given host and port
        /// </summary>
        /// <param name="host">The host</param>
        /// <param name="port">The port</param>
        [Obsolete]
        void Configure( string host, int port );

        /// <summary>
        /// Gets the announcement details
        /// </summary>
        IReadOnlyDictionary<string, string> AnnouncementDetails { get; }
    }
}
