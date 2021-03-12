using Grpc.Core;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes a component that manages a grpc server
    /// </summary>
    public interface IServerProvider
    {
        /// <summary>
        /// Gets the default credentials used for the server
        /// </summary>
        ServerCredentials Credentials { get; set; }

        /// <summary>
        /// Adds the given service definition to the server
        /// </summary>
        /// <param name="serviceDefinition">The service definition that should be added</param>
        void AddService( ServerServiceDefinition serviceDefinition );

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

        /// <summary>
        /// Starts the server
        /// </summary>
        void Start();

        /// <summary>
        /// Asynchronously shuts down the server
        /// </summary>
        /// <returns>A task that can be awaited</returns>
        Task ShutdownAsync();
    }
}
