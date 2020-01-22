using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NUnit.Framework;
using Tecan.Sila2.Authentication;
using Tecan.Sila2.Authentication.AuthenticationService;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.DynamicClient;
using Tecan.Sila2.IntegrationTests.TestServer;
using Tecan.Sila2.Locking;
using Tecan.Sila2.Locking.LockController;
using Tecan.Sila2.Server;
using Tecan.Sila2.Tests;

namespace Tecan.Sila2.IntegrationTests
{
    // Integration tests do not work on CI server, unfortunately
    [TestFixture( Category = "IntegrationTests" )]
    public class IntegrationTestFixture
    {
        private ServerData _server;
        private Process _serverProcess;

        [SetUp]
        public void StartTestServer()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "TestServer");
            var serverId = Guid.NewGuid();
            var payloadArguments = $"-u {serverId} -i 127.0.0.1";
            var attachDebugger = Debugger.IsAttached;
            if( attachDebugger )
            {
                payloadArguments = "debug " + payloadArguments;
            }
            var processStartInfo = new ProcessStartInfo()
            {
#if NETCOREAPP
                FileName = "dotnet",
                Arguments = Path.Combine(dir, "netcoreapp2.1", "TestServer.dll") + " " + payloadArguments,
#elif NET461
                FileName = Path.Combine(dir, "net461", "TestServer.exe"),
                Arguments = payloadArguments,
#elif NET472
                FileName = Path.Combine(dir, "net472", "TestServer.exe"),
                Arguments = payloadArguments,
#endif
                CreateNoWindow = true,
                ErrorDialog = false,
                RedirectStandardInput = true,
                RedirectStandardError = false,
                RedirectStandardOutput = false,
                UseShellExecute = false,
                WorkingDirectory = dir
            };
            _serverProcess = Process.Start(processStartInfo);

            if( attachDebugger )
            {
                Debugger.Break();
            }
            else
            {
                Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
            }

            var testLogging = new TestLoggingFactory();
            var connector = new ServerConnector( new DiscoveryExecutionManager(), testLogging );
            
            _server = connector.Connect( IPAddress.Loopback, 50052 );
            Assert.That( _server, Is.Not.Null );
        }

        [TearDown]
        public void StopTestServer()
        {
            if( _serverProcess != null && !_serverProcess.HasExited )
            {
                _serverProcess.StandardInput.WriteLine();
                _serverProcess.StandardInput.Flush();
                _serverProcess.WaitForExit( 1000 );
                if( !_serverProcess.HasExited )
                {
                    _serverProcess.Kill();
                }
            }
        }

        [Test]
        public void PassBoolean_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client(_server.Channel, executionManager);
            
            Assert.That(testClient.PassBool(true), Is.True);
            Assert.That(testClient.PassBool(false), Is.False);

            Unlock(lockId, token);
            Logout(token);
        }

        [Test]
        public void PassBooleanDynamic_TransmittedSuccessFully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server.Channel, executionManager );
            var commandClient = new NonObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "PassBool" ), featureContext );
            
            PassAndAssertEquals(commandClient, true);
            PassAndAssertEquals( commandClient, false );

            Unlock( lockId, token );
            Logout( token );

        }

        private static void PassAndAssertEquals(NonObservableCommandClient commandClient, object value)
        {
            var request = commandClient.CreateRequest();
            request.Value = new DynamicObject()
            {
                Elements =
                {
                    new DynamicObjectProperty(((StructureType) request.Type.Item).Element[0])
                    {
                        Value = value
                    }
                }
            };
            var response = commandClient.Invoke(request);
            Assert.That((response.Value as DynamicObject).Elements[0].Value, Is.EqualTo(value));
        }

        [Test]
        public void PassDouble_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client( _server.Channel, executionManager );

            Assert.That( testClient.PassDouble(0.0), Is.EqualTo(0.0) );
            Assert.That( testClient.PassDouble( 8.15 ), Is.EqualTo( 8.15 ) );
            Assert.That( testClient.PassDouble( -1.0 ), Is.EqualTo( -1.0 ) );
            Assert.That( testClient.PassDouble( double.NaN ), Is.EqualTo( double.NaN ) );
            Assert.That( testClient.PassDouble( double.PositiveInfinity ), Is.EqualTo( double.PositiveInfinity ) );
            Assert.That( testClient.PassDouble( double.Epsilon ), Is.EqualTo( double.Epsilon ) );
            Assert.That( testClient.PassDouble( double.MaxValue ), Is.EqualTo( double.MaxValue ) );
            Assert.That( testClient.PassDouble( double.MinValue ), Is.EqualTo( double.MinValue ) );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassDoubleDynamic_TransmittedSuccessFully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server.Channel, executionManager );
            var commandClient = new NonObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "PassDouble" ), featureContext );

            PassAndAssertEquals( commandClient, 0.0 );
            PassAndAssertEquals( commandClient, 8.15 );
            PassAndAssertEquals( commandClient, double.NaN );
            PassAndAssertEquals( commandClient, double.PositiveInfinity );
            PassAndAssertEquals( commandClient, double.NegativeInfinity );
            PassAndAssertEquals( commandClient, double.MaxValue );
            PassAndAssertEquals( commandClient, double.MinValue );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassLong_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client( _server.Channel, executionManager );

            Assert.That( testClient.PassLong( 0 ), Is.EqualTo( 0L ) );
            Assert.That( testClient.PassLong( 42 ), Is.EqualTo( 42L ) );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassLongDynamic_TransmittedSuccessFully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server.Channel, executionManager );
            var commandClient = new NonObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "PassLong" ), featureContext );

            PassAndAssertEquals( commandClient, 0L );
            PassAndAssertEquals( commandClient, 42L );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassInt_SubmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client( _server.Channel, executionManager );

            Assert.That( testClient.PassInt( 0 ), Is.EqualTo( 0 ) );
            Assert.That( testClient.PassInt( 42 ), Is.EqualTo( 42 ) );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassTimestamp_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client( _server.Channel, executionManager );

            // everything below seconds is not transmitted, so we cannot simply use now
            var timestamp = new DateTimeOffset(DateTime.Today, DateTimeOffset.Now.Offset);

            Assert.That( testClient.PassTimestamp( timestamp ), Is.EqualTo( timestamp ) );

            var maxTimestamp = new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.Zero);
            Assert.That(testClient.PassTimestamp(maxTimestamp), Is.EqualTo(maxTimestamp)  );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassTimestampDynamic_TransmittedSuccessFully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server.Channel, executionManager );
            var commandClient = new NonObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "PassTimestamp" ), featureContext );

            // everything below seconds is not transmitted, so we cannot simply use now
            var timestamp = new DateTimeOffset( DateTime.Today, DateTimeOffset.Now.Offset );
            var maxTimestamp = new DateTimeOffset( 9999, 12, 31, 23, 59, 59, TimeSpan.Zero );
            PassAndAssertEquals( commandClient, timestamp );
            PassAndAssertEquals( commandClient, maxTimestamp );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassTime_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client( _server.Channel, executionManager );

            Assert.That( testClient.PassTime( TimeSpan.Zero ), Is.EqualTo( TimeSpan.Zero ) );
            var timespan = new TimeSpan(0, 8, 15, 42);
            Assert.That( testClient.PassTime( timespan ), Is.EqualTo( timespan ) );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassTimeDynamic_TransmittedSuccessFully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server.Channel, executionManager );
            var commandClient = new NonObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "PassTime" ), featureContext );

            // everything below seconds is not transmitted, so we cannot simply use now
            var timespan = new TimeSpan( 0, 8, 15, 42 );
            PassAndAssertEquals( commandClient, TimeSpan.Zero );
            PassAndAssertEquals( commandClient, timespan );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassString_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client( _server.Channel, executionManager );

            Assert.That( testClient.PassString( "Foo" ), Is.EqualTo("Foo") );
            Assert.That( testClient.PassString( "֍Bar©" ), Is.EqualTo( "֍Bar©" ) );
            Assert.That( testClient.PassString( null ), Is.Null.Or.Empty );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassStringDynamic_TransmittedSuccessFully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server.Channel, executionManager );
            var commandClient = new NonObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "PassString" ), featureContext );

            PassAndAssertEquals( commandClient, "Foo" );
            PassAndAssertEquals( commandClient, "֍Bar©" );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassStructure_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client( _server.Channel, executionManager );

            var structure = new SomeStructure(true, 8.15, 42L, 23, DateTimeOffset.MinValue, new TimeSpan(0, 8, 15));
            Assert.That( testClient.PassStructure( structure ), Is.EqualTo(structure) );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassStructureDynamic_TransmittedSuccessFully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server.Channel, executionManager );
            var commandClient = new NonObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "PassStructure" ), featureContext );
            var structure = new SomeStructure( true, 8.15, 42L, 23, DateTimeOffset.MinValue, new TimeSpan( 0, 8, 15 ) );

            var request = commandClient.CreateRequest();
            var structureParameter = ((StructureType)request.Type.Item).Element[0];
            var structureType = (StructureType) featureContext.ResolveType( structureParameter.DataType.Item.ToString()).Item;
            request.Value = new DynamicObject()
            {
                Elements =
                {
                    new DynamicObjectProperty(((StructureType) request.Type.Item).Element[0])
                    {
                        Value = new DynamicObject()
                        {
                            Elements =
                            {
                                new DynamicObjectProperty(structureType.Element[0]) { Value = true }, // bool
                                new DynamicObjectProperty(structureType.Element[1]) {Value = 8.15}, // double
                                new DynamicObjectProperty(structureType.Element[2]) {Value = 42L}, // long
                                new DynamicObjectProperty(structureType.Element[3]) {Value = 23L}, // int
                                new DynamicObjectProperty(structureType.Element[4]) {Value = new DateTimeOffset(DateTime.Today)}, // timestamp
                                new DynamicObjectProperty(structureType.Element[5]) {Value = new TimeSpan(0, 8, 15)}, // timespan
                            }
                        }
                    }
                }
            };
            var response = commandClient.Invoke( request );
            var responseStructure = (DynamicObject)((DynamicObject)response.Value).Elements[0].Value;

            Assert.That(responseStructure.Elements[0].Value, Is.EqualTo(true));
            Assert.That( responseStructure.Elements[1].Value, Is.EqualTo( 8.15 ) );
            Assert.That( responseStructure.Elements[2].Value, Is.EqualTo( 42L ) );
            Assert.That( responseStructure.Elements[3].Value, Is.EqualTo( 23L ) );
            Assert.That( responseStructure.Elements[4].Value, Is.EqualTo( new DateTimeOffset( DateTime.Today ) ) );
            Assert.That( responseStructure.Elements[5].Value, Is.EqualTo( new TimeSpan( 0, 8, 15 ) ) );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassBytes_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client( _server.Channel, executionManager );

            PassDataToClientAndTest(testClient, 0, 8, 15);

            Unlock( lockId, token );
            Logout( token );
        }

        private static void PassDataToClientAndTest(TestServerProvider.Client testClient, params byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                ms.Position = 0;
                using (var response = testClient.PassBytes(ms))
                {
                    AssertData(bytes, response);
                }

                ms.Position = 0;
                using (var response = testClient.PassStream(ms))
                {
                    AssertData(bytes, response);
                }

                ms.Position = 0;
                using (var response = testClient.PassFile(ms))
                {
                    AssertData(bytes, response);
                }
            }
        }

        [Test]
        public void PassBytesDynamic_TransmittedSuccessFully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server.Channel, executionManager );
            var commandClient = new NonObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "PassBytes" ), featureContext );

            var value = new byte[] {0, 8, 15};
            var response = PassBytesThrough(commandClient, value);

            Assert.That(response, Is.EquivalentTo( value ) );

            Unlock( lockId, token );
            Logout( token );
        }

        private static byte[] PassBytesThrough(NonObservableCommandClient commandClient, byte[] value)
        {
            var request = commandClient.CreateRequest();
            request.Value = new DynamicObject()
            {
                Elements =
                {
                    new DynamicObjectProperty(((StructureType) request.Type.Item).Element[0])
                    {
                        Value = new MemoryStream(value)
                    }
                }
            };
            var response = commandClient.Invoke(request);
            var returnStream = (response.Value as DynamicObject).Elements[0].Value;
            Assert.That(returnStream, Is.InstanceOf(typeof(Stream)));
            using (var responseStream = new MemoryStream())
            {
                ((Stream) returnStream).CopyTo(responseStream);
                return responseStream.ToArray();
            }
        }

        [Test]
        public void PassCollection_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client( _server.Channel, executionManager );

            var listOfItems = new List<string>();
            Assert.That( testClient.PassCollection(listOfItems), Is.Null.Or.EquivalentTo(listOfItems) );
            listOfItems.Add("Foo");
            listOfItems.Add("Bar");
            Assert.That( testClient.PassCollection( listOfItems ), Is.EquivalentTo( listOfItems ) );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void PassCollectionDynamic_TransmittedSuccessFully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server.Channel, executionManager );
            var commandClient = new NonObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "PassCollection" ), featureContext );

            var listOfItems = new List<string>();
            PassAndAssertEquivalent(commandClient, listOfItems);
            listOfItems.Add("Foo");
            listOfItems.Add("Bar");
            PassAndAssertEquivalent( commandClient, listOfItems );

            Unlock( lockId, token );
            Logout( token );
        }

        private static void PassAndAssertEquivalent(NonObservableCommandClient commandClient, List<string> listOfItems)
        {
            var request = commandClient.CreateRequest();
            request.Value = new DynamicObject()
            {
                Elements =
                {
                    new DynamicObjectProperty(((StructureType) request.Type.Item).Element[0])
                    {
                        Value = listOfItems
                    }
                }
            };
            var response = commandClient.Invoke(request);
            if( listOfItems == null || listOfItems.Count == 0 )
            {
                // response may be null
                if( response.Value != null )
                {
                    Assert.That( (response.Value as DynamicObject).Elements, Is.Empty );
                }
            }
            else
            {
                Assert.That( (response.Value as DynamicObject).Elements[0].Value, Is.EquivalentTo( listOfItems ) );
            }
        }



        [Test]
        public void RunObservableCommand_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client( _server.Channel, executionManager );

            var stateUpdates = new List<StateUpdateEventArgs>();

            var command = testClient.RunObservable();
            command.StateUpdateReceived += ( o, e ) => { stateUpdates.Add(e); };
            command.Response.Wait(TimeSpan.FromMinutes(1));
            var result = command.Response.Result;
            Assert.That(result, Is.EqualTo(42L));
            Assert.That(stateUpdates.Count, Is.EqualTo(4));

            Unlock( lockId, token );
            Logout( token );
        }


        [Test]
        public void RunObservableCommandDynamic_TransmittedSuccessFully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server.Channel, executionManager );
            var commandClient = new ObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "RunObservable" ), featureContext );

            var stateUpdates = new List<StateUpdateEventArgs>();
            var command = commandClient.Invoke( commandClient.CreateRequest() );
            command.StateUpdateReceived += ( o, e ) => { stateUpdates.Add( e ); };
            command.Response.Wait( TimeSpan.FromMinutes( 1 ) );
            var result = command.Response.Result;
            Assert.That( ((DynamicObject)result.Value).Elements[0].Value, Is.EqualTo( 42L ) );
            Assert.That( stateUpdates.Count, Is.EqualTo( 4 ) );


            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void RunIntermediateCommand_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client( _server.Channel, executionManager );

            var intermediateResults = new List<long>();

            var command = testClient.RunIntermediateObservableCommand();
            command.IntermediateResponse += ( o, e ) => { intermediateResults.Add( e.Data ); };
            command.Response.Wait( TimeSpan.FromMinutes( 1 ) );
            Assert.That( intermediateResults, Is.EquivalentTo( new long[] { 0, 8, 15 } ) );

            Unlock( lockId, token );
            Logout( token );
        }


        [Test]
        public void RunIntermediateCommandDynamic_TransmittedSuccessFully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server.Channel, executionManager );
            var commandClient = new IntermediateObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "RunIntermediateObservableCommand" ), featureContext );

            var intermediateResults = new List<DynamicObjectProperty>();
            var command = commandClient.Invoke( commandClient.CreateRequest() );
            command.IntermediateResponse += ( o, e ) => { intermediateResults.Add( e.Data ); };
            command.Response.Wait( TimeSpan.FromMinutes( 1 ) );
            Assert.That(intermediateResults.Count, Is.EqualTo(3));

            Unlock( lockId, token );
            Logout( token );
        }

        private static void AssertData(byte[] expected, Stream response)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That((byte) response.ReadByte(), Is.EqualTo(expected[i]), $"Data at position {i} is wrong.");
            }

            Assert.That(response.ReadByte(), Is.EqualTo(-1));
        }

        [Test]
        public void InvalidLogin_ThrowsSilaException()
        {
            Assert.Throws<SilaException>( () => Lock( null ) );
            Assert.Throws<SilaException>( () => Lock( "bullshit" ) );
        }

        private string Login()
        {
            var authenticationClient = new AuthenticationServiceProvider.Client(_server.Channel, new DiscoveryExecutionManager());
            var token = authenticationClient.Login( "admin", "admin", _server.Config.Uuid.ToString(), new List<string>() );
            return token.AccessToken;
        }

        private string Lock( string token )
        {
            var lockingClient = new LockControllerProvider.Client(_server.Channel, new TestAuthLockingExecutionManager(null, token, _server.Channel));
            var lockIdentifier = Guid.NewGuid().ToString();
            lockingClient.LockServer(lockIdentifier, 3600);
            return lockIdentifier;
        }

        private void Unlock( string identifier, string token )
        {
            var lockingClient = new LockControllerProvider.Client( _server.Channel, new TestAuthLockingExecutionManager( null, token, _server.Channel ) );
            lockingClient.UnlockServer(identifier);
        }

        private void Logout( string token )
        {
            var authenticationClient = new AuthenticationServiceProvider.Client( _server.Channel, new DiscoveryExecutionManager() );
            authenticationClient.Logout( token );
        }
    }
}
