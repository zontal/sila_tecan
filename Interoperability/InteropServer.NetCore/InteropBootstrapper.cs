using Microsoft.AspNetCore.Server.Kestrel.Core;
using Tecan.Sila2.Server;

namespace InteropServer.NetCore
{
    public class InteropBootstrapper : DryBootstrapper
    {
        protected override void OnConfigureServices( IServiceCollection serviceCollection )
        {
            serviceCollection.AddRazorPages();
        }

        protected override void OnConfigureApplication( IApplicationBuilder app )
        {
            app.UseExceptionHandler( "/Error" );
            app.UseHsts();

            app.UseStaticFiles();
            app.UseRouting();
        }

        protected override void OnConfigureEndpoints( IEndpointRouteBuilder endpoints )
        {
            endpoints.MapRazorPages();
        }

        protected override void OnConfigureKestrel( KestrelServerOptions kestrel )
        {
            kestrel.ListenLocalhost( 9000, opts =>
            {
                opts.Protocols = HttpProtocols.Http1AndHttp2;
                opts.UseHttps();
            } );
        }

        protected override HttpProtocols Protocols => HttpProtocols.Http1AndHttp2;
    }
}
