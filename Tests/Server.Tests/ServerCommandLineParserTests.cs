using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using NUnit.Framework;

namespace Tecan.Sila2.Server.Tests
{
    /// <summary>
    /// Verifies that the reflection-free server command line parser behaves like CommandLine.Parser.Default of
    /// CommandLineParser 2.9.1, which the server used before, for <see cref="ServerCommandLineArguments"/>
    /// </summary>
    [TestFixture]
    public class ServerCommandLineParserTests
    {
        private const string Guid1 = "5d1a0b2e-0000-4000-8000-000000000001";
        private const string Defaults = " iface=* noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=null";

        private static readonly string[] Vocabulary =
        {
            "-p", "--port", "-i", "--interface", "--no-discovery", "-u", "--uuid", "--config-name", "-c", "--certificate", "-n", "--name",
            "--help", "--version", "-h", "-?", "--", "-", "---", "--=", "--=x", "--p", "--n=v", "--port=", "--port=1", "--port= 1",
            "--name=a=b", "--no-discovery=false", "--no-discovery=x", "-p5", "-pX", "-np", "-xp5", "-x", "-xy", "--foo", "--PORT", "--HELP",
            "-port", "-no-discovery", "1", "-1", "+2", " 3 ", "99999999999", "abc", Guid1, "{" + Guid1 + "}", "eth\\d", "", "true", "false",
            "TRUE", "help", "version", "/app/Server.dll", "a b", "-\u0663", "--name=a\nb", "p", "n", "c", "u", "i", "port", "name",
            "no-discovery", "--" + new string( 'w', 100 ), "Option with a rather long value that has to be wrapped in the help screen",
        };

        /// <summary>
        /// The command lines of the original parity analysis (first 42) and further cases that exercise the tokenizer,
        /// with the outcome CommandLineParser 2.9.1 produced for them
        /// </summary>
        public static IEnumerable<TestCaseData> RecordedCases()
        {
            yield return Case( "empty", new string[0], "OK port=null" + Defaults );
            yield return Case( "port short", new[] { "-p", "50052" }, "OK port=50052" + Defaults );
            yield return Case( "port long", new[] { "--port", "50052" }, "OK port=50052" + Defaults );
            yield return Case( "port long =", new[] { "--port=50052" }, "OK port=50052" + Defaults );
            yield return Case( "port short attached", new[] { "-p50052" }, "OK port=50052" + Defaults );
            yield return Case( "port bad", new[] { "--port", "abc" }, "ERROR BadFormatConversionError" );
            yield return Case( "port missing value", new[] { "--port" }, "OK port=null" + Defaults );
            yield return Case( "port negative", new[] { "-p", "-1" }, "OK port=-1" + Defaults );
            yield return Case( "port repeated", new[] { "-p", "1", "-p", "2" }, "ERROR RepeatedOptionError" );
            yield return Case( "no-discovery", new[] { "--no-discovery" }, "OK port=null iface=* noDisc=True uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=null" );
            yield return Case( "no-discovery true", new[] { "--no-discovery", "true" }, "OK port=null iface=* noDisc=True uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=null" );
            yield return Case( "no-discovery=false", new[] { "--no-discovery=false" }, "OK port=null iface=* noDisc=True uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=null" );
            yield return Case( "uuid", new[] { "-u", Guid1 }, "OK port=null iface=* noDisc=False uuid=5d1a0b2e-0000-4000-8000-000000000001 config=null cert=null name=null" );
            yield return Case( "uuid bad", new[] { "--uuid", "nope" }, "ERROR BadFormatConversionError" );
            yield return Case( "interface regex", new[] { "-i", "eth\\d" }, "OK port=null iface=eth\\d noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=null" );
            yield return Case( "interface cidr", new[] { "--interface", "192.168.1.0/24" }, "OK port=null iface=192.168.1.0/24 noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=null" );
            yield return Case( "name with space", new[] { "-n", "My Server" }, "OK port=null iface=* noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=My Server" );
            yield return Case( "config-name", new[] { "--config-name", "lab" }, "OK port=null iface=* noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=lab cert=null name=null" );
            yield return Case( "certificate", new[] { "-c", "/certs/server.pfx" }, "OK port=null iface=* noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=null cert=/certs/server.pfx name=null" );
            yield return Case( "all", new[] { "-p", "50052", "-i", "lo", "--no-discovery", "-u", Guid1, "--config-name", "lab", "-c", "c.pfx", "-n", "S" }, "OK port=50052 iface=lo noDisc=True uuid=5d1a0b2e-0000-4000-8000-000000000001 config=lab cert=c.pfx name=S" );
            yield return Case( "argv0 exe path", new[] { "/app/Server.dll", "-p", "50052" }, "OK port=50052" + Defaults );
            yield return Case( "argv0 only", new[] { "/app/Server.dll" }, "OK port=null" + Defaults );
            yield return Case( "unknown long", new[] { "--foo" }, "ERROR UnknownOptionError" );
            yield return Case( "unknown short", new[] { "-x" }, "ERROR UnknownOptionError" );
            yield return Case( "uppercase long", new[] { "--PORT", "1" }, "ERROR UnknownOptionError" );
            yield return Case( "response file token", new[] { "@args.rsp" }, "OK port=null" + Defaults );
            yield return Case( "double dash", new[] { "--", "-p", "1" }, "OK port=1" + Defaults );
            yield return Case( "name missing value", new[] { "--name" }, "OK port=null" + Defaults );
            yield return Case( "name then option", new[] { "-n", "-p", "5" }, "OK port=null iface=* noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=p" );
            yield return Case( "port then option", new[] { "-p", "-i", "lo" }, "ERROR BadFormatConversionError" );
            yield return Case( "short = form", new[] { "-p=50052" }, "ERROR BadFormatConversionError" );
            yield return Case( "short : form", new[] { "-p:50052" }, "ERROR BadFormatConversionError" );
            yield return Case( "empty value", new[] { "--name", "" }, "OK port=null iface=* noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=" );
            yield return Case( "long single dash", new[] { "-port", "1" }, "ERROR BadFormatConversionError" );
            yield return Case( "port overflow", new[] { "-p", "99999999999" }, "ERROR BadFormatConversionError" );
            yield return Case( "uuid no dashes", new[] { "-u", "5d1a0b2e000040008000000000000001" }, "OK port=null iface=* noDisc=False uuid=5d1a0b2e-0000-4000-8000-000000000001 config=null cert=null name=null" );
            yield return Case( "bool repeated", new[] { "--no-discovery", "--no-discovery" }, "ERROR RepeatedOptionError" );
            yield return Case( "help", new[] { "--help" }, "ERROR HelpRequestedError" );
            yield return Case( "help short", new[] { "-h" }, "ERROR UnknownOptionError" );
            yield return Case( "help question", new[] { "-?" }, "ERROR UnknownOptionError" );
            yield return Case( "version", new[] { "--version" }, "ERROR VersionRequestedError" );
            yield return Case( "help verb", new[] { "help" }, "OK port=null" + Defaults );
            yield return Case( "x help after argv0", new[] { "/app/Server.dll", "--help" }, "ERROR UnknownOptionError" );
            yield return Case( "x version after argv0", new[] { "/app/Server.dll", "--version" }, "ERROR UnknownOptionError" );
            yield return Case( "x help after option", new[] { "-p", "1", "--help" }, "ERROR UnknownOptionError" );
            yield return Case( "x help uppercase", new[] { "--HELP" }, "ERROR UnknownOptionError" );
            yield return Case( "x help then garbage", new[] { "--help", "--foo" }, "ERROR HelpRequestedError" );
            yield return Case( "x version then option", new[] { "--version", "-p", "1" }, "ERROR VersionRequestedError" );
            yield return Case( "x name then long=", new[] { "-n", "--port=5" }, "OK port=null iface=* noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=port" );
            yield return Case( "x name then attached", new[] { "-n", "-p5" }, "OK port=null iface=* noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=p" );
            yield return Case( "x name then unknown", new[] { "-n", "--foo" }, "ERROR UnknownOptionError" );
            yield return Case( "x name unknown value", new[] { "-n", "-x", "5" }, "ERROR UnknownOptionError" );
            yield return Case( "x long one-char alias", new[] { "--p", "5" }, "OK port=5" + Defaults );
            yield return Case( "x long one-char =", new[] { "--p=5" }, "ERROR BadFormatTokenError" );
            yield return Case( "x long = empty", new[] { "--port=" }, "ERROR BadFormatTokenError" );
            yield return Case( "x long = space", new[] { "--port= 5" }, "ERROR BadFormatTokenError" );
            yield return Case( "x long =x", new[] { "--=x" }, "ERROR UnknownOptionError" );
            yield return Case( "x long = in value", new[] { "--name=a=b" }, "OK port=null iface=* noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=a=b" );
            yield return Case( "x triple dash", new[] { "---" }, "ERROR UnknownOptionError" );
            yield return Case( "x single dash", new[] { "-" }, "OK port=null" + Defaults );
            yield return Case( "x name dash value", new[] { "-n", "-" }, "OK port=null iface=* noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=-" );
            yield return Case( "x name double dash", new[] { "-n", "--" }, "OK port=null" + Defaults );
            yield return Case( "x repeated short long", new[] { "-p", "1", "--port", "2" }, "ERROR RepeatedOptionError" );
            yield return Case( "x repeated last bad", new[] { "-p", "1", "-p", "x" }, "ERROR BadFormatConversionError" );
            yield return Case( "x repeated first bad", new[] { "-p", "x", "-p", "1" }, "ERROR RepeatedOptionError" );
            yield return Case( "x consumed then repeat", new[] { "-p", "5", "-n", "-p" }, "ERROR RepeatedOptionError" );
            yield return Case( "x unknown repeated", new[] { "-x", "-x" }, "ERROR UnknownOptionError" );
            yield return Case( "x short group unknown", new[] { "-xp5" }, "ERROR UnknownOptionError" );
            yield return Case( "x single dash long", new[] { "-no-discovery" }, "OK port=null iface=* noDisc=False uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=o-discovery" );
            yield return Case( "x port plus sign", new[] { "-p", "+5" }, "OK port=5" + Defaults );
            yield return Case( "x port whitespace", new[] { "-p", " 5 " }, "OK port=5" + Defaults );
            yield return Case( "x uuid braces", new[] { "-u", "{5d1a0b2e-0000-4000-8000-000000000001}" }, "OK port=null iface=* noDisc=False uuid=5d1a0b2e-0000-4000-8000-000000000001 config=null cert=null name=null" );
            yield return Case( "x uuid empty", new[] { "-u", "" }, "ERROR BadFormatConversionError" );
            yield return Case( "x many errors", new[] { "--foo", "-p", "x", "--name", "a", "--name", "b", "--bar", "--x=1", "-u", "q" }, "ERROR BadFormatTokenError,UnknownOptionError,UnknownOptionError,BadFormatConversionError,BadFormatConversionError,RepeatedOptionError" );
            yield return Case( "x newline value", new[] { "--name=a\nb" }, "ERROR BadFormatTokenError" );
            yield return Case( "x unicode digit", new[] { "-p", "-\u0663" }, "ERROR BadFormatConversionError" );
            yield return Case( "x long unknown wraps", new[] { "--" + new string('w', 100) }, "ERROR UnknownOptionError" );
            yield return Case( "x switch then value", new[] { "--no-discovery", "-p", "3", "stray" }, "OK port=3 iface=* noDisc=True uuid=00000000-0000-0000-0000-000000000000 config=null cert=null name=null" );
        }

        [TestCaseSource( nameof( RecordedCases ) )]
        public void Parse_ProducesRecordedOutcome( string[] args, string expectedOutcome )
        {
            var arguments = ServerCommandLineParser.Parse( args, out var errors );

            Assert.That( Describe( arguments, errors ), Is.EqualTo( expectedOutcome ) );
        }

        [TestCaseSource( nameof( RecordedCases ) )]
        public void Parse_BehavesLikeCommandLineParser( string[] args, string expectedOutcome )
        {
            AssertBehavesLikeCommandLineParser( args );
        }

        [Test]
        public void Parse_RandomCommandLines_BehavesLikeCommandLineParser()
        {
            var random = new Random( 4711 );
            for(var iteration = 0; iteration < 3000; iteration++)
            {
                var args = Enumerable.Range( 0, random.Next( 0, 9 ) ).Select( _ => Vocabulary[random.Next( Vocabulary.Length )] ).ToArray();
                AssertBehavesLikeCommandLineParser( args );
            }
        }

        [TestCase( 1 )]
        [TestCase( 10 )]
        [TestCase( 24 )]
        [TestCase( 30 )]
        [TestCase( 40 )]
        [TestCase( 60 )]
        [TestCase( 79 )]
        [TestCase( 80 )]
        [TestCase( 81 )]
        [TestCase( 120 )]
        [TestCase( 250 )]
        public void BuildHelpText_ForAnyDisplayWidth_MatchesCommandLineParser( int width )
        {
            var commandLines = new[]
            {
                new[] { "--help" },
                new[] { "--version" },
                new[] { "--foo", "-p", "x", "--name", "a", "--name", "b", "--x=1" },
                new[] { "--" + new string( 'w', 300 ) },
                new[] { "--name=a\nb" },
            };
            foreach(var args in commandLines)
            {
                var writer = new StringWriter();
                using(var parser = new Parser( settings =>
                {
                    settings.HelpWriter = writer;
                    settings.MaximumDisplayWidth = width;
                } ))
                {
                    parser.ParseArguments<ServerCommandLineArguments>( args );
                }

                ServerCommandLineParser.Parse( args, out var errors );

                Assert.That( ServerCommandLineParser.BuildHelpText( errors, width, Assembly.GetEntryAssembly() ), Is.EqualTo( writer.ToString() ), string.Join( " ", args ) );
            }
        }

        [Test]
        public void GetMaximumDisplayWidth_MatchesCommandLineParser()
        {
            Assert.That( ServerCommandLineParser.GetMaximumDisplayWidth(), Is.EqualTo( new ParserSettings().MaximumDisplayWidth ) );
        }

        [Test]
        public void ParseArguments_Help_WritesHelpScreenToStandardError()
        {
            var output = CaptureStandardError( () => ServerCommandLineParser.ParseArguments( new[] { "--help" }, _ => true, errors => errors.IsHelp() ), out var isHelp );

            Assert.That( isHelp, Is.True );
            Assert.That( output, Is.EqualTo( CommandLineParserHelpScreen( new[] { "--help" } ) ) );
            Assert.That( output, Does.Contain( "  -p, --port           Port to connect server to." ) );
        }

        [Test]
        public void ParseArguments_Version_WritesHeadingToStandardError()
        {
            var output = CaptureStandardError( () => ServerCommandLineParser.ParseArguments( new[] { "--version" }, _ => true, errors => errors.IsVersion() ), out var isVersion );

            Assert.That( isVersion, Is.True );
            Assert.That( output, Is.EqualTo( CommandLineParserHelpScreen( new[] { "--version" } ) ) );
        }

        [Test]
        public void ParseArguments_ValidCommandLine_WritesNothing()
        {
            var output = CaptureStandardError( () => ServerCommandLineParser.ParseArguments( new[] { "-p", "50052" }, arguments => arguments.Port, _ => null ), out var port );

            Assert.That( port, Is.EqualTo( 50052 ) );
            Assert.That( output, Is.Empty );
        }

        [Test]
        public void Parse_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>( () => ServerCommandLineParser.Parse( null, out _ ) );
        }

        [Test]
        public void Bootstrapper_Start_PassesParsedArgumentsToStartServer()
        {
            var bootstrapper = new RecordingBootstrapper();

            var started = Bootstrapper.Start( new[] { "-p", "50052", "--no-discovery", "-n", "Server" }, bootstrapper );

            Assert.That( started, Is.True );
            Assert.That( bootstrapper.Errors, Is.Null );
            Assert.That( bootstrapper.Arguments.Port, Is.EqualTo( 50052 ) );
            Assert.That( bootstrapper.Arguments.NoDiscovery, Is.True );
            Assert.That( bootstrapper.Arguments.Name, Is.EqualTo( "Server" ) );
            Assert.That( bootstrapper.Arguments.InterfaceFilter, Is.EqualTo( "*" ) );
        }

        [Test]
        public void Bootstrapper_Start_PassesErrorsToHandleErrorAfterWritingHelpScreen()
        {
            var args = new[] { "--foo", "-p", "x" };
            var bootstrapper = new RecordingBootstrapper();

            var output = CaptureStandardError( () => Bootstrapper.Start( args, bootstrapper ), out var started );

            Assert.That( started, Is.False );
            Assert.That( bootstrapper.Arguments, Is.Null );
            Assert.That( bootstrapper.Errors.Select( error => error.Tag ), Is.EqualTo( new[] { ErrorType.UnknownOptionError, ErrorType.BadFormatConversionError } ) );
            Assert.That( output, Is.EqualTo( CommandLineParserHelpScreen( args ) ) );
        }

        [Test]
        public void Bootstrapper_HandleError_SetsExitCode()
        {
            var exitCode = Environment.ExitCode;
            try
            {
                CaptureStandardError( () => Bootstrapper.Start( new[] { "--foo" }, new Bootstrapper() ), out var started );

                Assert.That( started, Is.False );
                Assert.That( Environment.ExitCode, Is.EqualTo( 1 ) );
            }
            finally
            {
                Environment.ExitCode = exitCode;
            }
        }

#if ManagedGrpc
        [Test]
        public void DryBootstrapper_Start_PassesParsedArgumentsToStartServer()
        {
            var bootstrapper = new RecordingDryBootstrapper();

            var started = DryBootstrapper.Start( new[] { "-u", Guid1, "--config-name", "lab" }, bootstrapper );

            Assert.That( started, Is.True );
            Assert.That( bootstrapper.Arguments.ServerUuid, Is.EqualTo( Guid.Parse( Guid1 ) ) );
            Assert.That( bootstrapper.Arguments.ConfigName, Is.EqualTo( "lab" ) );
        }

        [Test]
        public void DryBootstrapper_Start_PassesErrorsToHandleError()
        {
            var bootstrapper = new RecordingDryBootstrapper();

            var output = CaptureStandardError( () => DryBootstrapper.Start( new[] { "--version" }, bootstrapper ), out var started );

            Assert.That( started, Is.False );
            Assert.That( bootstrapper.Errors.IsVersion(), Is.True );
            Assert.That( output, Is.EqualTo( CommandLineParserHelpScreen( new[] { "--version" } ) ) );
        }

        [TestCase( "--foo" )]
        [TestCase( "--help" )]
        [TestCase( "-p", "1", "-p", "x", "--port", "y", "--bar" )]
        public void ServerConfigReader_InvalidCommandLine_ThrowsSameMessageAsBefore( params string[] args )
        {
            IEnumerable<Error> expectedErrors = null;
            using(var parser = new Parser( settings => settings.HelpWriter = null ))
            {
                parser.ParseArguments<ServerCommandLineArguments>( args ).WithNotParsed( errors => expectedErrors = errors );
            }

            InvalidOperationException exception = null;
            var output = CaptureStandardError( () => exception = Assert.Throws<InvalidOperationException>( () => ServerConfigReader.ReadServerStartInformation( args ) ), out _ );

            Assert.That( exception.Message, Is.EqualTo( "The server was started incorrectly: " + string.Join( ", ", expectedErrors ) ) );
            Assert.That( output, Is.EqualTo( CommandLineParserHelpScreen( args ) ) );
        }

        [Test]
        public void ServerConfigReader_ValidCommandLine_ReturnsStartInformation()
        {
            var info = ServerConfigReader.ReadServerStartInformation( new[] { "/app/Server.dll", "-p", "50052", "-i", "lo", "-u", Guid1, "-n", "Server", "--config-name", "lab" } );

            Assert.That( info.Port, Is.EqualTo( 50052 ) );
            Assert.That( info.NetworkInterfaces, Is.EqualTo( "lo" ) );
            Assert.That( info.ServerUuid, Is.EqualTo( Guid.Parse( Guid1 ) ) );
            Assert.That( info.Name, Is.EqualTo( "Server" ) );
            Assert.That( info.ConfigName, Is.EqualTo( "lab" ) );
        }
#endif

        private static TestCaseData Case( string name, string[] args, string expectedOutcome )
        {
            return new TestCaseData( args, expectedOutcome ).SetName( "{m}(" + name + ")" );
        }

        private static string Describe( ServerCommandLineArguments arguments, IEnumerable<Error> errors )
        {
            if(arguments == null)
            {
                return "ERROR " + string.Join( ",", errors.Select( error => error.Tag ) );
            }

            return $"OK port={arguments.Port?.ToString() ?? "null"} iface={arguments.InterfaceFilter ?? "null"} noDisc={arguments.NoDiscovery} uuid={arguments.ServerUuid} config={arguments.ConfigName ?? "null"} cert={arguments.CertificatePath ?? "null"} name={arguments.Name ?? "null"}";
        }

        private static void AssertBehavesLikeCommandLineParser( string[] args )
        {
            var commandLine = string.Join( " ", args.Select( arg => "'" + arg + "'" ) );
            var writer = new StringWriter();
            ParserResult<ServerCommandLineArguments> expected;
            int width;
            using(var parser = new Parser( settings => settings.HelpWriter = writer ))
            {
                width = parser.Settings.MaximumDisplayWidth;
                expected = parser.ParseArguments<ServerCommandLineArguments>( args );
            }

            var actual = ServerCommandLineParser.Parse( args, out var errors );

            if(expected is Parsed<ServerCommandLineArguments> parsed)
            {
                Assert.That( errors, Is.Empty, commandLine );
                Assert.That( Describe( actual, errors ), Is.EqualTo( Describe( parsed.Value, null ) ), commandLine );
                return;
            }

            var expectedErrors = ( (NotParsed<ServerCommandLineArguments>)expected ).Errors.ToList();
            Assert.That( actual, Is.Null, commandLine );
            Assert.That( errors.Select( DescribeError ), Is.EqualTo( expectedErrors.Select( DescribeError ) ), commandLine );
            Assert.That( ServerCommandLineParser.BuildHelpText( errors, width, Assembly.GetEntryAssembly() ), Is.EqualTo( writer.ToString() ), commandLine );
        }

        private static string DescribeError( Error error )
        {
            string details;
            switch(error)
            {
                case ServerCommandLineError serverError:
                    details = serverError.Token ?? ( serverError.LongName == null ? "" : serverError.ShortName + "|" + serverError.LongName );
                    break;
                case TokenError tokenError:
                    details = tokenError.Token;
                    break;
                case NamedError namedError:
                    details = namedError.NameInfo.ShortName + "|" + namedError.NameInfo.LongName;
                    break;
                default:
                    details = "";
                    break;
            }

            return $"{error} {error.Tag} {error.StopsProcessing} {details}";
        }

        private static string CommandLineParserHelpScreen( string[] args )
        {
            var writer = new StringWriter();
            using(var parser = new Parser( settings => settings.HelpWriter = writer ))
            {
                parser.ParseArguments<ServerCommandLineArguments>( args );
            }

            return writer.ToString();
        }

        private static string CaptureStandardError<T>( Func<T> action, out T result )
        {
            var original = Console.Error;
            var writer = new StringWriter();
            Console.SetError( writer );
            try
            {
                result = action();
            }
            finally
            {
                Console.SetError( original );
            }

            return writer.ToString();
        }

        private sealed class RecordingBootstrapper : Bootstrapper
        {
            public ServerCommandLineArguments Arguments { get; private set; }

            public List<Error> Errors { get; private set; }

            public override bool StartServer( ServerCommandLineArguments args )
            {
                Arguments = args;
                return true;
            }

            public override bool HandleError( IEnumerable<Error> obj )
            {
                Errors = obj.ToList();
                return false;
            }
        }

#if ManagedGrpc
        private sealed class RecordingDryBootstrapper : DryBootstrapper
        {
            public ServerCommandLineArguments Arguments { get; private set; }

            public List<Error> Errors { get; private set; }

            public override bool StartServer( ServerCommandLineArguments args )
            {
                Arguments = args;
                return true;
            }

            public override bool HandleError( IEnumerable<Error> obj )
            {
                Errors = obj.ToList();
                return false;
            }
        }
#endif
    }
}