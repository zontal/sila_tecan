using CommandLine;
using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Tecan.Sila2.Security;

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

            var info = new ServerStartInformation( selfInfo, arguments.Port, arguments.InterfaceFilter, arguments.ServerUuid, arguments.Name );

            if (arguments.CertificatePath != null)
            {
                try
                {
                    info.ServerCertificate = new X509Certificate2(arguments.CertificatePath);
                    if (CertificateUtilities.TryFindServerUuid(info.ServerCertificate, out var certUuid))
                    {
                        if (info.ServerUuid != Guid.Empty && info.ServerUuid != certUuid)
                        {
                            LogManager.GetLogger(typeof(ServerConfigReader)).Warn($"Server UUID from custom certificate {certUuid} does not match configured ID {info.ServerUuid}.");
                        }
                        info.ServerUuid = certUuid;
                    }
                }
                catch (Exception ex)
                {
                    LogManager.GetLogger(typeof(ServerConfigReader)).Warn($"Could not load certificate {arguments.CertificatePath}: {ex.Message}", ex);
                }
            }

            if (info.ServerCertificate == null)
            {
                if (info.ServerUuid == Guid.Empty)
                {
                    info.ServerUuid = Guid.NewGuid();
                }
                info.ServerCertificate = CertificateGenerator.GetDefaultCertificate(info.ServerUuid);
            }

            return info;
        }
    }
}
