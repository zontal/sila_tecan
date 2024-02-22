using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using CommandLine;
using Common.Logging;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.CommandLine
{
    [Export( typeof( ICommandLineVerb ) )]
    [Verb( "generate-certificate", HelpText = "Helper command to generate self-signed certificates" )]
    internal class GenerateCertificatesVerb : VerbBase
    {
        private static readonly ILog _loggingChannel = LogManager.GetLogger<GenerateCertificatesVerb>();

        [Option( 'a', "ca-path", Required = true, HelpText = "The path to the certificate authority key file" )]
        public string CAKeyPath { get; set; }

        [Option( "ca-cert", Required = false, HelpText = "The path to the certificate authority certificate, if this is created from scratch" )]
        public string CAPath { get; set; }

        [Option( 'p', "ca-password", Required = true, HelpText = "The pass-phrase for the certificate authority" )]
        public string CAPassword { get; set; }

        [Value( 1, HelpText = "The directory where the server certificate should be stored", Required = false,
            Default = "." )]
        public string OutputPath { get; set; } = ".";

        [Option( 'd', "duration", HelpText = "The duration of the certificate in days", Required = false, Default = 3650 )]
        public int Duration { get; set; } = 10;

        [Option( 'c', "country", HelpText = "The country that should appear in the certificate. " )]
        public string Country { get; set; }

        [Option( 'l', "location", HelpText = "The location of the certificate." )]
        public string Location { get; set; }

        [Option( 'o', "organization", HelpText = "The company name as in the certificate." )]
        public string Company { get; set; }

        [Option( 'u', "unit", HelpText = "The organizational unit where the certificate comes from." )]
        public string OrganizationUnit { get; set; }

        [Value( 0, HelpText = "The target server UUID for which the certificate is issued.", Required = true )]
        public string TargetName { get; set; }

        [Option('n', "hosts", HelpText = "Subject alternative names to put into the certificate")]
        public IEnumerable<string> Hosts { get; set; }

        public override void Execute( CompositionContainer container )
        {
            var channel = SetupLogging( container );
            if(Country == null)
            {
                channel.Debug( "No country provided, using current region" );
                Country = RegionInfo.CurrentRegion.TwoLetterISORegionName;
            }
            Environment.CurrentDirectory = Path.GetFullPath( OutputPath );
            EnsureCaExists();
            // force create new server key
            var serverTempPassword = "pass:1111";
            OpenSsl( $"genrsa -passout {serverTempPassword} -des3 -out server.key", "Generate server key" );
            var subject = CreateSubject( "SiLA2" );
            var config = CreateConfig( TargetName, false );
            try
            {
                OpenSsl( $"req -passin {serverTempPassword} -new -key server.key -out server.csr -subj {subject} -config \"{config}\"", "Create server signing request" );
                OpenSsl( $"x509 -req -passin {CAPassword} -days {Duration} -in server.csr -CA ca.crt -CAkey \"{CAKeyPath}\" -set_serial 01 -out server.crt -extfile \"{config}\" -extensions v3_req", "Create server certificate" );
            }
            finally
            {
                File.Delete( config );
            }
            OpenSsl( $"rsa -passin {serverTempPassword} -in server.key -out server.key", "Remove passphrase from server key" );
        }

        private string CreateConfig( string serverUUID, bool isCa )
        {
            var fileName = Path.GetTempFileName();
            using(var writer = new StreamWriter( fileName ))
            {
                writer.WriteLine( "[req]" );
                if(!isCa)
                {
                    writer.WriteLine( "req_extensions = v3_req" );
                    writer.WriteLine( "x509_extensions = v3_req" );
                }
                writer.WriteLine( "distinguished_name = req_distinguished_name" );
                writer.WriteLine( "[req_distinguished_name]" );
                if (!isCa)
                {
                    writer.WriteLine("[v3_req]");
                    writer.WriteLine("basicConstraints = CA:FALSE");
                    writer.WriteLine("keyUsage = digitalSignature, keyEncipherment");
                    if (!string.IsNullOrEmpty(serverUUID))
                    {
                        writer.WriteLine($"1.3.6.1.4.1.58583=critical,ASN1:UTF8String:{serverUUID}");
                        writer.WriteLine($"1.3.6.1.4.1.58583=ASN1:UTF8String:{serverUUID}");
                    }
                    if (Hosts != null && Hosts.Any())
                    {
                        writer.WriteLine("subjectAltName = " + string.Join(",", Hosts.Select(PrintSANHost)));
                    }
                }
            }
            return fileName;
        }

        private string PrintSANHost(string host)
        {
            if (IPAddress.TryParse(host, out _))
            {
                return "IP:" + host;
            }
            return "DNS:" + host;
        }

        private void EnsureCaExists()
        {
            if(Path.GetExtension( CAKeyPath ).EndsWith( "crt" ))
            {
                _loggingChannel.Warn( "The key file for the certificate authority looks like a certificate (.crt). Key files by convention carry the .key extension." );
            }
            if(!File.Exists( CAKeyPath ))
            {
                OpenSsl( $"genrsa -passout {CAPassword} -des3 -out \"{CAKeyPath}\" 4096", "Creating CA key" );
            }
            else
            {
                _loggingChannel.Debug( "CA key found" );
            }
            var caCrt = CAPath ?? Path.ChangeExtension( CAKeyPath, ".crt" );
            if(!File.Exists( caCrt ))
            {
                var subject = CreateSubject( "CA" );
                var config = CreateConfig( null, true );
                try
                {
                    OpenSsl( $"req -passin {CAPassword} -new -x509 -days {Duration} -key \"{CAKeyPath}\" -out \"{caCrt}\" -subj \"{subject}\" -config \"{config}\"", "Create CA certificate" );
                }
                finally
                {
                    File.Delete( config );
                }
            }
            else
            {
                _loggingChannel.Debug( "CA crt found" );
            }

            if(Path.GetDirectoryName( Path.GetFullPath( caCrt ) ) != Path.GetFullPath( OutputPath ) || Path.GetFileName( caCrt ) != "ca.crt")
            {
                _loggingChannel.Debug( "Copy CA.crt" );
                File.Copy( caCrt, Path.Combine( OutputPath, "ca.crt" ) );
            }
            else
            {
                _loggingChannel.Debug( $"CA.crt is already located at {OutputPath}" );
            }
        }

        private string CreateSubject( string commonName )
        {
            var subject = new StringBuilder();
            if(!string.IsNullOrWhiteSpace( Country ))
            {
                subject.Append( $"/C={Country}" );
            }

            if(!string.IsNullOrWhiteSpace( Location ))
            {
                subject.Append( $"/L={Location}" );
            }

            if(!string.IsNullOrWhiteSpace( Company ))
            {
                subject.Append( $"/O={Company}" );
            }

            if(!string.IsNullOrWhiteSpace( OrganizationUnit ))
            {
                subject.Append( $"/OU={OrganizationUnit}" );
            }

            if(!string.IsNullOrWhiteSpace( commonName ))
            {
                subject.Append( $"/CN={commonName}" );
            }

            return subject.ToString();
        }

        private void OpenSsl( string arguments, string actionDescription )
        {
            _loggingChannel.Info( actionDescription );
            var startInfo = new ProcessStartInfo( "openssl", arguments );
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            var process = Process.Start( startInfo );
            process.WaitForExit();
            _loggingChannel.Debug( process.StandardOutput.ReadToEnd() );
            var error = process.StandardError.ReadToEnd();
            if(!string.IsNullOrEmpty( error ))
            {
                _loggingChannel.Error( error );
            }
            if(process.ExitCode != 0)
            {
                _loggingChannel.Warn( $"{actionDescription} failed." );
            }
        }
    }
}
