using DryIoc;
using DryIoc.MefAttributedModel;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using Tecan.Sila2.Security;
using Tecan.Sila2.Server.Binary;
using Tecan.Sila2.Server.ServerPooling;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Exposes extension methods to include DryIoc into
    /// </summary>
    public static class DryIoCExtensions
    {
        /// <summary>
        /// Instructs the host to use a DryIoC container with the given configuration callback
        /// </summary>
        /// <param name="builder">The host builder</param>
        /// <param name="configureContainer">A callback that is used to configure the container.</param>
        /// <returns>The host builder for chaining</returns>
        public static IHostBuilder UseDryIoc( this IHostBuilder builder, Action<IContainer> configureContainer )
        {
            var container = new Container().WithMef();
            configureContainer?.Invoke( container );
#if NET10_0
            container.RegisterInstance(typeof(TimeProvider), TimeProvider.System);
#endif
            return builder.UseServiceProviderFactory( new DryIocServiceProviderFactory( container ) );
        }

        /// <summary>
        /// Loads the exports contained in assemblies in the given folder into the provided container
        /// </summary>
        /// <param name="container">The DryIoc container</param>
        /// <param name="path">The folder path</param>
        /// <param name="filter">A filter or null to use the default filter *.dll</param>
        public static void LoadComponents( this IContainer container, string path, string filter = null )
        {
            foreach(var assemblyFile in Directory.EnumerateFiles( path, filter ?? "*.dll" ))
            {
                try
                {
                    LoadComponentsFromAssembly( container, Assembly.LoadFrom( Path.Combine( path, assemblyFile ) ) );
                }
                catch(BadImageFormatException)
                {
                    // assembly is not a .NET DLL and therefore safe to ignore
                }
            }
        }

        /// <summary>
        /// Loads the exports contained in assemblies in the application binary directory into the provided container
        /// </summary>
        /// <param name="container">The DryIoc container</param>
        /// <param name="filter">A filter or null to use the default filter *.dll</param>
        public static void LoadComponentsFromApplicationDirectory( this IContainer container, string filter = null )
        {
            var assemblyLocation = Assembly.GetEntryAssembly().Location;
            var path = Path.GetDirectoryName( assemblyLocation );
            container.LoadComponents( path, filter );
        }

        /// <summary>
        /// Loads the exports from the given assembly into the provided container
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="assembly">The assembly that contains the components</param>
        public static void LoadComponentsFromAssembly( this IContainer container, Assembly assembly )
        {
            container.RegisterExports( assembly );
        }

        /// <summary>
        /// Adds default implementations for several infrastructure services needed from the SiLA2 SDK in case they are missing
        /// </summary>
        /// <param name="container">The container to which the elements should be added</param>
        public static void AddSila2Defaults( this IContainer container )
        {
            if(!container.IsRegistered<IFileManagement>())
            {
                container.RegisterInstance<IFileManagement>( new FileManagement() );
            }

            if(!container.IsRegistered<IConfigurationStore>())
            {
                container.RegisterInstance<IConfigurationStore>( new FileConfigurationStore() );
            }

            if(!container.IsRegistered<IServerCertificateProvider>())
            {
                container.RegisterInstance<IServerCertificateProvider>( new FileCertificateProvider() );
            }

            if(!container.IsRegistered<IServerPoolConnectionFactory>())
            {
                container.RegisterInstance<IServerPoolConnectionFactory>( new DefaultServerPoolConnectionFactory() );
            }
        }
    }
}
