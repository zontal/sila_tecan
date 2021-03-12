using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using CommandLine;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.CommandLine
{
    [Export(typeof(ICommandLineVerb))]
    [Verb("generate-certificate", HelpText = "Helper command to generate self-signed certificates")]
    internal class GenerateCertificatesVerb : VerbBase
    {
        [Option('a', "ca-path", Required = true, HelpText = "The path to the certificate authority key file")]
        public string CAPath { get; set; }

        [Option('p', "ca-password", Required = true, HelpText = "The pass-phrase for the certificate authority")]
        public string CAPassword { get; set; }

        [Value(1, HelpText = "The directory where the server certificate should be stored", Required = false,
            Default = ".")]
        public string OutputPath { get; set; } = ".";

        [Option('d', "duration", HelpText = "The duration of the certificate in days", Required = false, Default = 3650)]
        public int Duration { get; set; } = 10;

        [Option('c', "country", HelpText = "The country that should appear in the certificate. ")]
        public string Country { get; set; }

        [Option('l', "location", HelpText = "The location of the certificate.")]
        public string Location { get; set; }

        [Option('o', "organization", HelpText = "The company name as in the certificate.")]
        public string Company { get; set; }

        [Option('u', "unit", HelpText = "The organizational unit where the certificate comes from.")]
        public string OrganizationUnit { get; set; }

        [Value(0, HelpText = "The target server UUID for which the certificate is issued.", Required = true)]
        public string TargetName { get; set; }

        public override void Execute(CompositionContainer container)
        {
            var channel = SetupLogging( container );
            if (Country == null)
            {
                channel.Log( "No country provided, using current region".ToString );
                Country = RegionInfo.CurrentRegion.TwoLetterISORegionName;
            }
            Environment.CurrentDirectory = Path.GetFullPath(OutputPath);
            EnsureCaExists(channel);
            // force create new server key
            var serverTempPassword = "pass:1111";
            OpenSsl(channel, $"genrsa -passout {serverTempPassword} -des3 -out server.key", "Generate server key");
            var subject = CreateSubject("SiLA2");
            var config = CreateConfig( TargetName );
            try
            {
                OpenSsl( channel, $"req -passin {serverTempPassword} -new -key server.key -out server.csr -subj {subject} -config \"{config}\"", "Create server signing request" );
                OpenSsl( channel, $"x509 -req -passin {CAPassword} -days {Duration} -in server.csr -CA ca.crt -CAkey \"{CAPath}\" -set_serial 01 -out server.crt -extfile \"{config}\" -extensions v3_req", "Create server certificate" );
            }
            finally
            {
                File.Delete( config );
            }
            OpenSsl( channel, $"rsa -passin {serverTempPassword} -in server.key -out server.key", "Remove passphrase from server key");
        }

        private string CreateConfig(string serverUUID)
        {
            var fileName = Path.GetTempFileName();
            using(var writer = new StreamWriter( fileName ))
            {
                writer.WriteLine( "[req]" );
                writer.WriteLine( "req_extensions = v3_req" );
                writer.WriteLine( "x509_extensions = v3_req" );
                writer.WriteLine( "distinguished_name = req_distinguished_name" );
                writer.WriteLine( "[req_distinguished_name]" );
                writer.WriteLine( "[v3_req]" );
                writer.WriteLine( "basicConstraints = CA:FALSE" );
                writer.WriteLine( "keyUsage = digitalSignature, keyEncipherment" );
                if(!string.IsNullOrEmpty( serverUUID ))
                {
                    writer.WriteLine( $"1.3.536=critical,ASN1:UTF8String:{serverUUID}" );
                    writer.WriteLine( $"1.3.536=ASN1:UTF8String:{serverUUID}" );
                }
            }
            return fileName;
        }

        private void EnsureCaExists(ILoggingChannel channel)
        {
            if (!File.Exists(CAPath))
            {
                OpenSsl( channel, $"genrsa -passout {CAPassword} -des3 -out \"{CAPath}\" 4096", "Creating CA key");
            }
            else
            {
                channel.Log("CA found".ToString);
            }
            var caCrt = Path.ChangeExtension(CAPath, ".crt");
            if (!File.Exists(caCrt))
            {
                var subject = CreateSubject("CA");
                var config = CreateConfig( null );
                try
                {
                    OpenSsl( channel, $"req -passin {CAPassword} -new -x509 -days {Duration} -key \"{CAPath}\" -out \"{caCrt}\" -subj \"{subject}\" -config \"{config}\"", "Create CA certificate" );
                }
                finally
                {
                    File.Delete( config );
                }
            }
            else
            {
                channel.Log("CA crt found".ToString);
            }

            if (Path.GetDirectoryName(Path.GetFullPath(caCrt)) != Path.GetFullPath(OutputPath) || Path.GetFileName(caCrt) != "ca.crt")
            {
                channel.Log("Copy CA.crt".ToString);
                File.Copy(caCrt, Path.Combine(OutputPath, "ca.crt"));
            }
            else
            {
                channel.Log($"CA.crt is already located at {OutputPath}".ToString);
            }
        }

        private string CreateSubject(string commonName)
        {
            var subject = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(Country))
            {
                subject.Append($"/C={Country}");
            }

            if (!string.IsNullOrWhiteSpace(Location))
            {
                subject.Append($"/L={Location}");
            }

            if (!string.IsNullOrWhiteSpace(Company))
            {
                subject.Append($"/O={Company}");
            }

            if (!string.IsNullOrWhiteSpace(OrganizationUnit))
            {
                subject.Append($"/OU={OrganizationUnit}");
            }

            if (!string.IsNullOrWhiteSpace(commonName))
            {
                subject.Append($"/CN={commonName}");
            }

            return subject.ToString();
        }

        private void OpenSsl(ILoggingChannel channel, string arguments, string actionDescription)
        {
            channel.Log(actionDescription.ToString, Severity.Info);
            var startInfo = new ProcessStartInfo("openssl", arguments);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            var process = Process.Start(startInfo);
            process.WaitForExit();
            channel.Log(process.StandardOutput.ReadToEnd);
            var error = process.StandardError.ReadToEnd();
            if( !string.IsNullOrEmpty( error ) )
            {
                channel.Log(error.ToString, Severity.Error);
            }
            if (process.ExitCode != 0)
            {
                channel.Log($"{actionDescription} failed.".ToString, Severity.Warning);
            }
        }
    }
}
