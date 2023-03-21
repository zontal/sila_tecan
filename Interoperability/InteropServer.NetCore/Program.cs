using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Tecan.Sila2.Server;

var serverInfo = ServerConfigReader.ReadServerStartInformation(new [] {"-i", "127.0.0.1"});
var serverId = Environment.GetEnvironmentVariable( "ServerID" );
if(Guid.TryParse( serverId, out var serverUuid ))
{
    serverInfo.ServerUuid = serverUuid;
}

if(int.TryParse( Environment.GetEnvironmentVariable( "Port" ), out var port ))
{
    serverInfo.Port = port;
}

var builder = WebApplication.CreateBuilder( args );
// Add services to the container.
builder.Host.UseDryIoc( container =>
 {
     container.LoadComponentsFromApplicationDirectory();
     container.AddSila2Defaults();
 } );
builder.Services.AddRazorPages();

builder.Services.AddSila2( serverInfo, opt => opt.IgnoreUnknownServices = true );

builder.WebHost.ConfigureKestrelForSila2( serverInfo, options =>
 {
     // you can also set this to Http1AndHttp2 but then Http2 only works with Https
     options.Protocols = HttpProtocols.Http2;
 } );

var app = builder.Build();
app.Services.InitializeLogging();

// Configure the HTTP request pipeline.
if(!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler( "/Error" );
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapSila2();

app.Run();