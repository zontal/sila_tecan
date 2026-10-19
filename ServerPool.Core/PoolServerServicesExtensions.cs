using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Tecan.Sila2.Client;
using Tecan.Sila2.Server;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Denotes extension methods to add a pool server to an existing ASP.NET Core server
    /// </summary>
    public static class PoolServerServicesExtensions
    {
        /// <summary>
        /// Adds Pool Server services
        /// </summary>
        /// <param name="services">The service collection to which the SiLA2 services shall be added to</param>
        public static void AddSila2ServerPool( this IServiceCollection services )
        {
            services.AddSila2ServerPool( null );
        }

        /// <summary>
        /// Adds Pool Server services
        /// </summary>
        /// <param name="services">The service collection to which the SiLA2 services shall be added to</param>
        /// <param name="configureOptions">A callback for gRPC configuration options</param>
        /// <returns>A gRPC server builder</returns>
        public static IGrpcServerBuilder AddSila2ServerPool( this IServiceCollection services, Action<GrpcServiceOptions> configureOptions )
        {
            var builder = configureOptions != null ? services.AddGrpc( configureOptions ) : services.AddGrpc();
            services.TryAddEnumerable( ServiceDescriptor.Singleton( typeof( IServiceMethodProvider<> ), typeof( ServiceMethodProvider<> ) ) );
            return builder;
        }

        /// <summary>
        /// Maps endpoints created from SiLA2 Server Pools
        /// </summary>
        /// <param name="builder">The endpoint builder</param>
        /// <returns>An endpoint convention builder object</returns>
        public static IEndpointConventionBuilder MapSila2ServerPool( this IEndpointRouteBuilder builder )
        {
            return builder.MapGrpcService<IServerPool>();
        }

        // TService carries the member requirement of Grpc.AspNetCore.Server's IServiceMethodProvider<TService>
        // (ServerDynamicAccessConstants.ServiceAccessibility), so a trimmed or Native AOT build keeps what the gRPC binder reflects over.
        private sealed class ServiceMethodProvider<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods )] TService> : IServiceMethodProvider<TService> where TService : class
        {
            private readonly IServiceConfigurationBuilder<IServerPool, ServiceMethodProviderContext<IServerPool>> _serviceHandlerRepository;

            public ServiceMethodProvider( IServiceConfigurationBuilder<IServerPool, ServiceMethodProviderContext<IServerPool>> serviceHandlerRepository )
            {
                _serviceHandlerRepository = serviceHandlerRepository;
            }

            void IServiceMethodProvider<TService>.OnServiceMethodDiscovery( ServiceMethodProviderContext<TService> context )
            {
                if(typeof( TService ) == typeof( IServerPool ))
                {
                    var castedContext = (ServiceMethodProviderContext<IServerPool>)(object)context;
                    foreach(var serviceHandler in _serviceHandlerRepository.ServiceHandlers)
                    {
                        serviceHandler.Register( castedContext );
                    }
                }
            }
        }
    }
}
