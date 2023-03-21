using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.Authentication;
using Tecan.Sila2.Client;
using Tecan.Sila2.Client.ExecutionManagement;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.DynamicClient;
using Tecan.Sila2.Interop.Client.AuthenticationTest;
using Tecan.Sila2.Interop.Client.AuthenticationTest.AuthenticationTest;
using Tecan.Sila2.Interop.Client.BinaryTransferTest;
using Tecan.Sila2.Interop.Client.BinaryTransferTest.BinaryTransferTest;
using Tecan.Sila2.Interop.Client.ErrorHandlingTest;
using Tecan.Sila2.Interop.Client.ErrorHandlingTest.ErrorHandlingTest;
using Tecan.Sila2.Interop.Client.MetadataConsumerTest.MetadataConsumerTest;
using Tecan.Sila2.Interop.Client.MetadataProvider;
using Tecan.Sila2.Interop.Client.ObservableCommandTest.ObservableCommandTest;
using Tecan.Sila2.Interop.Client.ObservablePropertyTest;
using Tecan.Sila2.Interop.Client.ObservablePropertyTest.ObservablePropertyTest;
using Tecan.Sila2.Interop.Client.UnobservableCommandTest;
using Tecan.Sila2.Interop.Client.UnobservableCommandTest.UnobservableCommandTest;
using Tecan.Sila2.Interop.Client.UnobservablePropertyTest.UnobservablePropertyTest;

namespace Tecan.Sila2.Interop.Client
{
    class Program
    {
        private static IExecutionManagerFactory _executionManagerFactory;
        private readonly IClientExecutionManager _executionManager;
        private readonly IClientChannel _channel;

        static void Main( string[] args )
        {
            var port = Environment.GetEnvironmentVariable( "Port" ) ?? "50052";
            var serverAddress = Environment.GetEnvironmentVariable( "ServerAddress" ) ?? "127.0.0.1";

#if MANAGED_GRPC
            var channel = Grpc.Net.Client.GrpcChannel.ForAddress($"http://{serverAddress}:{port}");
#else
            var channel = new Grpc.Core.Channel( serverAddress, int.Parse( port ), ChannelCredentials.Insecure );
#endif

            _executionManagerFactory = new ExecutionManagerFactory(new IClientRequestInterceptor[]
            {
                new StringMetadataInterceptor(),
                new TwoIntegersMetadataInterceptor(),
                new BinaryTestStringInterceptor(),
                new AuthenticationInterceptor(new AuthenticationHandler(), null, new Lazy<IExecutionManagerFactory>(() => _executionManagerFactory))
            });

            var connector = new ServerConnector( new DiscoveryExecutionManager() );
            var server = connector.GetServerData( channel );
            var executionManager = _executionManagerFactory.CreateExecutionManager( server );

            var program = new Program( server.Channel, executionManager );
            program.Run();
        }

        private void Run()
        {
            RunTest(TestSilaService);
            RunTest(TestUnobservableCommand);
            RunTest(TestUnobservableProperty);
            RunTest(TestMetadataConsumer);
            RunTest(TestErrorHandling);
            RunTest(TestObservableProperty);
            RunTest(TestObservableCommand);
            RunTest( TestBinaryTransfer );
            RunTest(TestAuthentication);
        }

        private void RunTest( Action test )
        {
            var testName = test.Method.Name;
            try
            {
                Console.WriteLine( $"Running {testName}" );
                test();
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine( ex.Message );
                Environment.ExitCode = 1;
            }
        }

        private void Assert<T>( T expected, T actual )
        {
            if(!EqualityComparer<T>.Default.Equals( expected, actual ))
            {
                Console.Error.WriteLine( $"Expected {expected} but got {actual}" );
                Environment.ExitCode = 1;
            }
        }

        private T AssertException<T>( Action action, bool allowAggregate = false ) where T : Exception
        {
            try
            {
                action();
                Console.Error.WriteLine( "No exception raised" );
                Environment.ExitCode = 1;
                return default( T );
            }
            catch(T expectedException)
            {
                return expectedException;
            }
            catch(AggregateException aggregateException)
            {
                if (allowAggregate && aggregateException.InnerException is T exc)
                {
                    return exc;
                }
                Console.Error.WriteLine($"Expected {typeof(T).Name}, but received {aggregateException.InnerException.GetType().Name}");
                Console.Error.WriteLine(aggregateException.InnerException.Message);
                Environment.ExitCode = 1;
                return default(T);
            }
            catch(Exception otherException)
            {
                Console.Error.WriteLine( $"Expected {typeof( T ).Name}, but received {otherException.GetType().Name}" );
                Console.Error.WriteLine( otherException.Message );
                Environment.ExitCode = 1;
                return default( T );
            }
        }

        private void TestAuthentication()
        {
            var client = new AuthenticationTestClient(_channel, _executionManager);

            using (new AuthenticationScope())
            {
                client.RequiresToken();
                client.RequiresTokenForBinaryUpload(CreateStream("abc"));
                client.RequiresTokenForBinaryUpload(CreateStream(Repeat("abc", 1_000_000)));
            }
        }

        private void TestBinaryTransfer()
        {
            var client = new BinaryTransferTestClient(_channel, _executionManager);

            Assert("SiLA2_Test_String_Value", ReadStream(client.BinaryValueDirectly));
            AssertRepetition(client.BinaryValueDownload, "A_slightly_longer_SiLA2_Test_String_Value_used_to_demonstrate_the_binary_download", 100_000);

            Assert("abc", ReadStream(client.EchoBinaryValue(CreateStream("abc"))));
            AssertRepetition(client.EchoBinaryValue(CreateStream(Repeat("abc", 1_000_000))), "abc", 1_000_000);

            var echo1 = client.EchoBinaryAndMetadataString(CreateStream("abc"));
            var echo2 = client.EchoBinaryAndMetadataString(CreateStream(Repeat("abc", 1_000_000)));

            Assert("abc", ReadStream(echo1.Binary));
            Assert("abc", echo1.StringMetadata);
            AssertRepetition( echo2.Binary, "abc", 1_000_000);
            Assert("abc", echo2.StringMetadata);

            var echoObservably = client.EchoBinariesObservably(new List<Stream>()
                { 
                    CreateStream("abc"), 
                    CreateStream(Repeat("abc", 1_000_000)),
                    CreateStream("SiLA2_Test_String_Value")
                });
            echoObservably.Response.Wait();
            var echo = ReadStream(echoObservably.Response.Result);
            Assert(Repeat("abc", 1_000_001) + "SiLA2_Test_String_Value", echo);
        }

        private static Stream CreateStream(string contents)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes( contents ));
            ms.Position = 0;
            return ms;
        }

        private static string Repeat(string text, int n)
        {
            var sb = new StringBuilder(text.Length * n);
            for (int i = 0; i < n; i++)
            {
                sb.Append(text);
            }
            return sb.ToString();
        }

        private static string ReadStream(Stream stream)
        {
            var reader = new StreamReader( stream );
            return reader.ReadToEnd();
        }

        private void AssertRepetition(Stream stream, string repeatedString, int n)
        {
            var actual = ReadStream(stream);
            Assert(n * repeatedString.Length, actual.Length);
            if (actual.Length == n * repeatedString.Length)
            {
                for (int i = 0; i < n; i++)
                {
                    Assert(actual.Substring(i * repeatedString.Length, repeatedString.Length), repeatedString);
                }
            }
        }

        private void TestObservableCommand()
        {
            var client = new ObservableCommandTestClient( _channel, _executionManager );

            var count = client.Count( 5, 1 );
            var fetchIntermediates = FetchAll( count.IntermediateValues );
            var fetchExecutionStates = FetchAll( count.StateUpdates );
            Task.WaitAll( fetchIntermediates, fetchExecutionStates, count.Response );

            var numbers = fetchIntermediates.Result;

            if (numbers.Count == 4)
            {
                Console.Error.WriteLine("For some reason, the first number of counting was missed.");
                for (int i = 0; i < 4; i++)
                {
                    Assert(i + 1, numbers[i]);
                }
            }
            else
            {
                Assert(5, numbers.Count);
                for (int i = 0; i < 5; i++)
                {
                    Assert(i, numbers[i]);
                }
            }

            var echo = client.EchoValueAfterDelay( 3, 5 );
            fetchExecutionStates = FetchAll( echo.StateUpdates );
            Task.WaitAll( fetchExecutionStates, echo.Response );
            Assert( 3, echo.Response.Result );
        }

        private async Task<List<T>> FetchAll<T>( ChannelReader<T> channel )
        {
            var result = new List<T>();
            await foreach(var item in channel.ReadAllAsync())
            {
                result.Add( item );
            }
            return result;
        }

        private void TestObservableProperty()
        {
            using(var client = new ObservablePropertyTestClient( _channel, _executionManager ))
            {
                Assert( 42, client.FixedValue );

                var alternating = new List<bool>();
                for(int i = 0; i < 3; i++)
                {
                    alternating.Add( client.Alternating );
                    Thread.Sleep( 1000 );
                }
                Assert( false, alternating.All( v => v ) );
                Assert( false, alternating.All( v => !v ) );

                var receivedValues = new List<long>();
                // need to show interest into observable property
                var initialValue = client.Editable;
                client.PropertyChanged += ( o, e ) =>
                {
                    if(e.PropertyName == nameof( IObservablePropertyTest.Editable ))
                    {
                        receivedValues.Add( client.Editable );
                    }
                };
                client.SetValue( 1 );
                client.SetValue( 2 );
                client.SetValue( 3 );
                Thread.Sleep( 1000 );
                Assert( 3, receivedValues.Count );
                Assert( 1, receivedValues[0] );
                Assert( 2, receivedValues[1] );
                Assert( 3, receivedValues[2] );
            }
        }

        private void TestErrorHandling()
        {
            using(var client = new ErrorHandlingTestClient( _channel, _executionManager ))
            {
                AssertException<TestErrorException>( client.RaiseDefinedExecutionError );
                AssertException<SilaException>( client.RaiseUndefinedExecutionError );
                AssertException<TestErrorException>( () => _ = client.RaiseDefinedExecutionErrorOnGet );
                AssertException<SilaException>( () => _ = client.RaiseUndefinedExecutionErrorOnGet );
                AssertException<TestErrorException>( () => client.RaiseDefinedExecutionErrorObservably().Response.Wait(), true );
                AssertException<SilaException>( () => client.RaiseUndefinedExecutionErrorObservably().Response.Wait(), true );
                AssertException<DefinedErrorException>( () => _ = client.RaiseDefinedExecutionErrorOnSubscribe );
                AssertException<SilaException>( () => _ = client.RaiseUndefinedExecutionErrorOnSubscribe );

                Assert(1, client.RaiseDefinedExecutionErrorAfterValueWasSent );
                Assert(1, client.RaiseUndefinedExecutionErrorAfterValueWasSent);
            }
        }

        private void TestMetadataConsumer()
        {
            StringMetadataInterceptor.Metadata = "abc";
            var client = new MetadataConsumerTestClient( _channel, _executionManager );
            Assert( "abc", client.EchoStringMetadata() );
            TwoIntegersMetadataInterceptor.Metadata = new TwoIntegersMetadata( 123, 456 );
            var response = client.UnpackMetadata();
            Assert( "abc", response.ReceivedString );
            Assert( 123, response.FirstReceivedInteger );
            Assert( 456, response.SecondReceivedInteger );
        }

        private void TestUnobservableProperty()
        {
            var client = new UnobservablePropertyTestClient( _channel, _executionManager );
            Assert( 42, client.AnswerToEverything );
            Assert( true, client.SecondsSince1970 > 1000 );
        }

        private void TestUnobservableCommand()
        {
            var client = new UnobservableCommandTestClient( _channel, _executionManager );
            client.CommandWithoutParametersAndResponses();
            Assert( "12345", client.ConvertIntegerToString( 12345 ) );
            Assert( "123abc", client.JoinIntegerAndString( 123, "abc" ) );

            void AssertSplitString( string expectedFirst, string expectedSecond, SplitStringAfterFirstCharacterResponse actual )
            {
                Assert( expectedFirst, actual.FirstCharacter );
                Assert( expectedSecond, actual.Remainder );
            }
            AssertSplitString( "", "", client.SplitStringAfterFirstCharacter( "" ) );
            AssertSplitString( "a", "", client.SplitStringAfterFirstCharacter( "a" ) );
            AssertSplitString( "a", "b", client.SplitStringAfterFirstCharacter( "ab" ) );
            AssertSplitString( "a", "bcde", client.SplitStringAfterFirstCharacter( "abcde" ) );
        }

        private void TestSilaService()
        {
            var client = new SiLAServiceClient( _channel, _executionManager );
            client.SetServerName( "SiLA is Awesome" );
            // other calls are done implicitly during connection
        }

        public Program( IClientChannel channel, IClientExecutionManager executionManager )
        {
            _channel = channel;
            _executionManager = executionManager;
        }
    }
}
