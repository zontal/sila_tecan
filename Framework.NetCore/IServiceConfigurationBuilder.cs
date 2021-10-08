using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace Tecan.Sila2.Server
{
    public interface IServiceConfigurationBuilder<T> where T : class
    {
        void AddServiceHandler( IServiceHandler<T> serviceHandler );

        IEnumerable<IServiceHandler<T>> ServiceHandlers { get; }

        /// <summary>
        /// Configures the server for the given server unique identifier
        /// </summary>
        /// <param name="guid">The unique identifier of the server</param>
        /// <param name="port">The port on which the server should be running</param>
        /// <param name="networkInterfaceFilter"></param>
        void ConfigureForServer( Guid guid, int port, Predicate<NetworkInterface> networkInterfaceFilter );

        /// <summary>
        /// Configures the server to accept connections from the given host and port
        /// </summary>
        /// <param name="host">The host</param>
        /// <param name="port">The port</param>
        void Configure( string host, int port );
    }
}
