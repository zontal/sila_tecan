using Microsoft.AspNetCore.Server.Kestrel.Core;
using Tecan.Sila2.Interop.Server.BasicDataTypesTest;
using Tecan.Sila2.Server;

var startInfo = ServerConfigReader.ReadServerStartInformation(args);
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrelForSila2(startInfo, options => options.Protocols = HttpProtocols.Http2);

// it is important to call AddSila2Components in order to add these components that also have a MEF registration
builder.Services.AddSila2Components();
builder.Services.AddSila2(startInfo);

// since the default DI container of ASP.NET Core does not recognize MEF attributes,
// you will need to add the feature provider explicitly. Same applies for the interface implementation
builder.Services.AddSingleton<IBasicDataTypesTest, BasicDataTypesTest>();
builder.Services.AddSingleton<IFeatureProvider, BasicDataTypesTestProvider>();

var app = builder.Build();

app.Services.InitializeLogging();
app.MapSila2();

app.Run();
