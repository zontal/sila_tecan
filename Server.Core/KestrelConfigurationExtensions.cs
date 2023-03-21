using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes a helper class to configure Kestrel according to a provided server start information
    /// </summary>
    public static class KestrelConfigurationExtensions
    {
        /// <summary>
        /// Configures Kestrel according to the provided server start information
        /// </summary>
        /// <param name="hostBuilder">the host builder</param>
        /// <param name="serverStartInfo">the server start information</param>
        /// <param name="connectionOptions">the connection options to use</param>
        public static void ConfigureKestrelForSila2( this IHostBuilder hostBuilder, ServerStartInformation serverStartInfo, Action<ListenOptions> connectionOptions )
        {
            hostBuilder.ConfigureWebHostDefaults( webBuilder =>
             {
                 webBuilder.ConfigureKestrelForSila2( serverStartInfo, connectionOptions );
             } );
        }


        /// <summary>
        /// Configures Kestrel according to the provided server start information
        /// </summary>
        /// <param name="webBuilder">the host builder</param>
        /// <param name="serverStartInfo">the server start information</param>
        /// <param name="connectionOptions">the connection options to use</param>
        public static void ConfigureKestrelForSila2( this IWebHostBuilder webBuilder, ServerStartInformation serverStartInfo, Action<ListenOptions> connectionOptions )
        {
            webBuilder.ConfigureKestrel( options =>
             {
                 var port = (serverStartInfo?.Port).GetValueOrDefault( ServerStartInformation.DefaultPort );
                 if(serverStartInfo?.NetworkInterfaces != null)
                 {
                     foreach(var net in Networking.ListInterNetworkAddresses( Networking.CreateNetworkInterfaceFilter( serverStartInfo.NetworkInterfaces ), false ))
                     {
                         options.Listen( net, port, connectionOptions );
                     }
                 }
                 else
                 {
                     options.ListenAnyIP( port, connectionOptions );
                 }
             } );
        }
    }
}
