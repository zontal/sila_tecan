using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Server;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SilaServicesExtensions
    {
        public static void AddSila2(this IServiceCollection services)
        {
            services.AddSila2( null );
        }

        public static IGrpcServerBuilder AddSila2( this IServiceCollection services, Action<GrpcServiceOptions> configureOptions )
        {
            var builder = configureOptions != null ? services.AddGrpc(configureOptions) : services.AddGrpc();
            services.TryAddEnumerable( ServiceDescriptor.Singleton( typeof( IServiceMethodProvider<> ), typeof( ServiceMethodProvider<> ) ) );
            return builder;
        }

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
            private readonly IServiceHandlerRepository<ISiLAServer> _serviceHandlerRepository;

            public ServiceMethodProvider(IServiceHandlerRepository<ISiLAServer> serviceHandlerRepository )
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
