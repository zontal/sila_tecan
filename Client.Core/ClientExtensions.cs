using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Client.ExecutionManagement;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Denotes extension methods to register client classes
    /// </summary>
    public static class ClientExtensions
    {
        /// <summary>
        /// Adds services for SiLA2 clients
        /// </summary>
        /// <param name="services">The service collection to which the SiLA2 services shall be added to</param>
        public static void AddSila2Client(this IServiceCollection services)
        {
            services.TryAddSingleton<IServerConnector>(sp => new ServerConnector(new DiscoveryExecutionManager()));
            services.TryAddSingleton<IExecutionManagerFactory, ExecutionManagerFactory>();
            services.TryAddSingleton<IClientProvider, ClientProvider>();
            services.TryAddSingleton<IServerDiscovery, ServerDiscovery>();
        }
    }
}
