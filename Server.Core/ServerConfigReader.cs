using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Helper class that is used to read the configuration of the server
    /// </summary>
    public static class ServerConfigReader
    {
        /// <summary>
        /// Reads the server start information from the command line parameters
        /// </summary>
        /// <returns>The appropriate server start information</returns>
        public static ServerStartInformation ReadServerStartInformation()
        {
            return ReadServerStartInformation( Environment.GetCommandLineArgs() );
        }

        /// <summary>
        /// Reads the server start information from the command line parameters
        /// </summary>
        /// <param name="args">The command line parameters</param>
        /// <returns>The appropriate server start information</returns>
        public static ServerStartInformation ReadServerStartInformation( string[] args )
        {
            return Parser.Default.ParseArguments<ServerCommandLineArguments>( args )
                .MapResult( CreateServerStartInfo, HandleError );
        }

        private static ServerStartInformation HandleError( IEnumerable<Error> arg )
        {
            throw new InvalidOperationException( $"The server was started incorrectly: " + string.Join( ", ", arg ) );
        }

        private static ServerStartInformation CreateServerStartInfo( ServerCommandLineArguments arguments )
        {
            var selfInfo = ServerInformationFactory.GetServerInformation();

            return new ServerStartInformation( selfInfo, arguments.Port, arguments.InterfaceFilter, arguments.ServerUuid, arguments.Name );
        }
    }
}
