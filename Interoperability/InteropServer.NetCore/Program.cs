using CommandLine;
using InteropServer.NetCore;
using Tecan.Sila2.Server;

var bootstrapper = new InteropBootstrapper();

if(Parser.Default.ParseArguments<ServerCommandLineArguments>( args )
    .MapResult( parsedArgs =>
    {
        var serverId = Environment.GetEnvironmentVariable( "ServerID" );
        if(Guid.TryParse( serverId, out var serverUuid ))
        {
            parsedArgs.ServerUuid = serverUuid;
        }

        if(int.TryParse( Environment.GetEnvironmentVariable( "Port" ), out var port ))
        {
            parsedArgs.Port = port;
        }
        return bootstrapper.StartServer( parsedArgs );
    }, bootstrapper.HandleError ))
{
    Console.ReadLine();
}