using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.CommandLine
{
    [Export(typeof(ICommandLineVerb))]
    [Verb("generate-certificate", HelpText = "Helper command to generate self-signed certificates")]
    internal class GenerateCertificatesVerb : ICommandLineVerb
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

        [Option('u', "The organizational unit where the certificate comes from.")]
        public string OrganizationUnit { get; set; }

        [Value(0, HelpText = "The target name for which the certificate is issued.", Required = true)]
        public string TargetName { get; set; }

        public void Execute(CompositionContainer container)
        {
            if (Country == null)
            {
                Country = RegionInfo.CurrentRegion.TwoLetterISORegionName;
            }
            Environment.CurrentDirectory = Path.GetFullPath(OutputPath);
            EnsureCaExists();
            // force create new server key
            var serverTempPassword = "pass:1111";
            OpenSsl($"genrsa -passout {serverTempPassword} -des3 -out server.key", "Generate server key");
            var subject = CreateSubject(TargetName);
            OpenSsl($"req -passin {serverTempPassword} -new -key server.key -out server.csr -subj {subject}", "Create server signing request");
            OpenSsl($"x509 -req -passin {CAPassword} -days {Duration} -in server.csr -CA ca.crt -CAkey \"{CAPath}\" -set_serial 01 -out server.crt", "Create server certificate");
            OpenSsl($"rsa -passin {serverTempPassword} -in server.key -out server.key", "Remove passphrase from server key");
        }

        private void EnsureCaExists()
        {
            if (!File.Exists(CAPath))
            {
                OpenSsl($"genrsa -passout {CAPassword} -des3 -out \"{CAPath}\" 4096", "Creating CA key");
            }
            var caCrt = Path.ChangeExtension(CAPath, ".crt");
            if (!File.Exists(caCrt))
            {
                var subject = CreateSubject("CA");

                OpenSsl($"req -passin {CAPassword} -new -x509 -days {Duration} -key \"{CAPath}\" -out \"{caCrt}\" -subj \"{subject}\"", "Create CA certificate");
            }

            if (Path.GetDirectoryName(Path.GetFullPath(caCrt)) != Path.GetFullPath(OutputPath) || Path.GetFileName(caCrt) != "ca.crt")
            {
                File.Copy(caCrt, Path.Combine(OutputPath, "ca.crt"));
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

        private void OpenSsl(string arguments, string actionDescription)
        {
            var startInfo = new ProcessStartInfo("openssl", arguments);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            var process = Process.Start(startInfo);
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.Error.WriteLine(process.StandardError.ReadToEnd());
            if (process.ExitCode != 0)
            {
                throw new Exception($"{actionDescription} failed, aborting certificate generation.");
            }
        }
    }
}
