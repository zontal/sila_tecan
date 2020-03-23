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

            Parser.Default.ParseArguments<ServerCommandLineArguments>( args )
                .WithNotParsed( bootstrapper.HandleError )
                .WithParsed( parsedArgs =>
                {
                    var serverId = Environment.GetEnvironmentVariable( "ServerID" );
                    if( Guid.TryParse( serverId, out var serverUuid ) )
                    {
                        parsedArgs.ServerUuid = serverUuid;
                    }

                    if( int.TryParse( Environment.GetEnvironmentVariable( "Port"), out var port ) )
                    {
                        parsedArgs.Port = port;
                    }
                    bootstrapper.StartServer( parsedArgs );
                } );
            Console.ReadLine();
        }
    }
}
