using DryIoc;
using DryIoc.MefAttributedModel;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Tecan.Sila2.Server
{
    public static class DryIoCExtensions
    {
        public static IHostBuilder UseDryIoc(this IHostBuilder builder, Action<IContainer> configureContainer)
        {
            var container = new Container().WithMef();
            configureContainer?.Invoke( container );
            return builder.UseServiceProviderFactory( new DryIocServiceProviderFactory( container ) );
        }

        /// <summary>
        /// Loads the exports contained in assemblies in the given folder into the provided container
        /// </summary>
        /// <param name="container">The DryIoc container</param>
        /// <param name="path">The folder path</param>
        /// <param name="filter">A filter or null to use the default filter *.dll</param>
        public static void LoadComponents( this IContainer container, string path, string filter )
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
        /// Loads the exports from the given assembly into the provided container
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="assembly">The assembly that contains the components</param>
        public static void LoadComponentsFromAssembly( this IContainer container, Assembly assembly )
        {
            container.RegisterExports( assembly );
        }
    }
}
