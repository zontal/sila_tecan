using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.Server;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Denotes extension methods to register SiLA2 Servers
    /// </summary>
    public static class SilaServicesExtensions
    {
        /// <summary>
        /// Adds SiLA2 services
        /// </summary>
        /// <param name="services">The service collection to which the SiLA2 services shall be added to</param>
        public static void AddSila2(this IServiceCollection services)
        {
            services.AddSila2( null );
        }

        /// <summary>
        /// Adds SiLA2 services
        /// </summary>
        /// <param name="services">The service collection to which the SiLA2 services shall be added to</param>
        /// <param name="configureOptions">A callback for gRPC configuration options</param>
        /// <returns>A gRPC server builder</returns>
        public static IGrpcServerBuilder AddSila2( this IServiceCollection services, Action<GrpcServiceOptions> configureOptions )
        {
            var builder = configureOptions != null ? services.AddGrpc(configureOptions) : services.AddGrpc();
            services.TryAddEnumerable( ServiceDescriptor.Singleton( typeof( IServiceMethodProvider<> ), typeof( ServiceMethodProvider<> ) ) );
            return builder;
        }

        /// <summary>
        /// Maps endpoints created from SiLA2 services
        /// </summary>
        /// <param name="builder">The endpoint builder</param>
        /// <returns>An endpoint convention builder object</returns>
        public static IEndpointConventionBuilder MapSila2( this IEndpointRouteBuilder builder )
        {
            var silaServer = builder.ServiceProvider.GetRequiredService<ISiLAServer>();
            var features = builder.ServiceProvider.GetServices<IFeatureProvider>();
            foreach(var feature in features)
            {
                silaServer.AddFeature( feature );
            }
            silaServer.StartServer();
            return builder.MapGrpcService<ISiLAServer>();
        }


        private sealed class ServiceMethodProvider<TService> : IServiceMethodProvider<TService> where TService : class
        {
            private readonly IServiceConfigurationBuilder<ISiLAServer> _serviceHandlerRepository;

            public ServiceMethodProvider(IServiceConfigurationBuilder<ISiLAServer> serviceHandlerRepository )
            {
                _serviceHandlerRepository = serviceHandlerRepository;
            }

            void IServiceMethodProvider<TService>.OnServiceMethodDiscovery( ServiceMethodProviderContext<TService> context )
            {
                if(typeof( TService ) == typeof( ISiLAServer ))
                {
                    var castedContext = (ServiceMethodProviderContext<ISiLAServer>)(object)context;
                    foreach(var serviceHandler in _serviceHandlerRepository.ServiceHandlers)
                    {
                        serviceHandler.Register( castedContext );
                    }
                }
            }
        }
    }
}
