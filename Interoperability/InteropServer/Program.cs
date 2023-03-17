using System;
using CommandLine;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Interop.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var bootstrapper = new Bootstrapper();

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
                    
                    parsedArgs.InterfaceFilter = "127.0.0.1";
                    
                    return bootstrapper.StartServer( parsedArgs );
                }, bootstrapper.HandleError ))
            {
                Console.ReadLine();
            }
        }
    }
}
