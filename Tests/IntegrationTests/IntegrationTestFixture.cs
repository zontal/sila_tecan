using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using NUnit.Framework;
using Tecan.Sila2.Authentication;
using Tecan.Sila2.Authentication.AuthenticationService;
using Tecan.Sila2.Cancellation;
using Tecan.Sila2.Cancellation.CancelController;
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
            var attachDebugger = false; // Debugger.IsAttached;
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
            var testClient = new TestServerClient( executionManager, _server.Channel);
            
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
            var featureContext = new FeatureContext( feature, _server, executionManager );
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
            var testClient = new TestServerClient( executionManager, _server.Channel );

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
            var featureContext = new FeatureContext( feature, _server, executionManager );
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
            var testClient = new TestServerClient( executionManager, _server.Channel );

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
            var featureContext = new FeatureContext( feature, _server, executionManager );
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
            var testClient = new TestServerClient( executionManager, _server.Channel );

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
            var testClient = new TestServerClient( executionManager, _server.Channel );

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
            var featureContext = new FeatureContext( feature, _server, executionManager );
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
            var testClient = new TestServerClient( executionManager, _server.Channel );

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
            var featureContext = new FeatureContext( feature, _server, executionManager );
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
            var testClient = new TestServerClient( executionManager, _server.Channel );

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
            var featureContext = new FeatureContext( feature, _server, executionManager );
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
            var testClient = new TestServerClient( executionManager, _server.Channel );

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
            var featureContext = new FeatureContext( feature, _server, executionManager );
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

        private static byte[] CreateLargeByteArray()
        {
            var result = new byte[1024 * 1024];
            var random = new Random();
            random.NextBytes( result );
            return result;
        }

        [Test]
        public void PassBytes_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerClient( executionManager, _server.Channel );

            PassDataToClientAndTest(testClient, 0, 8, 15);
            PassDataToClientAndTest( testClient, CreateLargeByteArray() );

            Unlock( lockId, token );
            Logout( token );
        }

        private static void PassDataToClientAndTest(TestServerClient testClient, params byte[] bytes)
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
            var featureContext = new FeatureContext( feature, _server, executionManager );
            var commandClient = new NonObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "PassBytes" ), featureContext );

            PassBytesThroughAndAssert(commandClient, new byte[] {0, 8, 15});
            PassBytesThroughAndAssert(commandClient, CreateLargeByteArray());

            Unlock( lockId, token );
            Logout( token );
        }

        private static void AssertArraysEqual(byte[] response, byte[] value)
        {
            Assert.That(response.Length, Is.EqualTo(value.Length));
            for (int i = 0; i < response.Length; i++)
            {
                Assert.That(response[i], Is.EqualTo(value[i]));
            }
        }

        private static void PassBytesThroughAndAssert(NonObservableCommandClient commandClient, byte[] value)
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
                var responseArray = responseStream.ToArray();

                // we cannot use Is.EquivalentTo for larger byte arrays because it is super-slow, rather check sequence equality explicitly
                AssertArraysEqual( responseArray, value );
            }
        }

        [Test]
        public void PassCollection_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerClient( executionManager, _server.Channel );

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
            var featureContext = new FeatureContext( feature, _server, executionManager );
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
            var testClient = new TestServerClient( executionManager, _server.Channel );
            
            var command = testClient.RunObservable();
            var stateUpdates = ReadChannel( command.StateUpdates );
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
            var featureContext = new FeatureContext( feature, _server, executionManager );
            var commandClient = new ObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == nameof(ITestServer.RunObservable) ), featureContext );

            var command = commandClient.Invoke( commandClient.CreateRequest() );
            var stateUpdates = ReadChannel( command.StateUpdates );
            command.Response.Wait( TimeSpan.FromMinutes( 1 ) );
            var result = command.Response.Result;
            Assert.That( ((DynamicObject)result.Value).Elements[0].Value, Is.EqualTo( 42L ) );
            Assert.That( stateUpdates.Count, Is.EqualTo( 4 ) );


            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void CancelObservableCommand_CancellationSuccessful()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerClient( executionManager, _server.Channel );
            var command = testClient.WaitForCancelOrThrow( TimeSpan.FromMinutes( 1 ) ) as IClientCommand;
            Assert.That(command, Is.Not.Null);
            Assert.That(command.IsCancellationSupported, Is.True);

            var cancelClient = new CancelControllerClient( executionManager, _server.Channel );
            cancelClient.CancelCommand( command.CommandId );

            Assert.ThrowsAsync<OperationCanceledException>( () => command.Response );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void CancelObservableCommandDynamic_CancellationSuccessful()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerClient( executionManager, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server, executionManager );
            var commandClient = new ObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == nameof(ITestServer.WaitForCancelOrThrow) ), featureContext );
            var request = commandClient.CreateRequest();
            request.Value = new DynamicObject()
            {
                Elements =
                {
                    new DynamicObjectProperty(((StructureType) request.Type.Item).Element[0])
                    {
                        Value = TimeSpan.FromMinutes(1)
                    }
                }
            };
            var command = commandClient.Invoke( request );
            command.Cancel();

            Assert.ThrowsAsync<OperationCanceledException>( () => command.Response );
            Assert.That(testClient.IsCancelled(command.CommandId.CommandId), Is.True);

            Unlock( lockId, token );
            Logout( token );
        }

        private static async Task<List<T>> ReadChannelCore<T>( ChannelReader<T> channel )
        {
            var values = new List<T>();
            while(await channel.WaitToReadAsync())
            {
                if(channel.TryRead( out var item ))
                {
                    values.Add( item );
                }
            }

            return values;
        }

        private static List<T> ReadChannel<T>( ChannelReader<T> channel )
        {
            var task = ReadChannelCore( channel );
            task.Wait(TimeSpan.FromMinutes(1));
            return task.Result;
        }

        [Test]
        public void RunIntermediateCommand_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerClient( executionManager, _server.Channel );
            
            var command = testClient.RunIntermediateObservableCommand();
            var intermediateResults = ReadChannel( command.IntermediateValues );
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
            var featureContext = new FeatureContext( feature, _server, executionManager );
            var commandClient = new IntermediateObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == "RunIntermediateObservableCommand" ), featureContext );

            var command = commandClient.Invoke( commandClient.CreateRequest() );
            var intermediateResults = ReadChannel( command.IntermediateValues );
            command.Response.Wait( TimeSpan.FromMinutes( 1 ) );
            Assert.That(intermediateResults.Count, Is.EqualTo(3));

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void UnobservableCommand_RunsIntoDefinedException_TranslatedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerClient( executionManager, _server.Channel );

            Assert.Throws<FakeException>( () => testClient.RunIntoFailure() );

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void ObservableCommand_RunsIntoDefinedException_TranslatedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerClient( executionManager, _server.Channel );
            var command = testClient.RunIntoFailureObservably();

            Assert.ThrowsAsync<FakeException>( () => command.Response );

            Unlock( lockId, token );
            Logout( token );

        }


        [Test]
        public void UnobservableCommandDynamic_RunsIntoDefinedException_TranslatedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server, executionManager );
            var commandClient = new NonObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == nameof(ITestServer.RunIntoFailure) ), featureContext );
            
            var exception = Assert.ThrowsAsync<DefinedErrorException>( () => commandClient.InvokeAsync( commandClient.CreateRequest() ) );
            var fake = feature.Items.OfType<FeatureDefinedExecutionError>().Single( e => e.Identifier == "Fake" );
            Assert.That(exception.ErrorIdentifier, Is.EqualTo(feature.GetFullyQualifiedIdentifier(fake)));

            Unlock( lockId, token );
            Logout( token );
        }

        [Test]
        public void ObservableCommandDynamic_RunsIntoDefinedException_TranslatedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server, executionManager );
            var commandClient = new ObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == nameof( ITestServer.RunIntoFailureObservably ) ), featureContext );
            var command = commandClient.Invoke( commandClient.CreateRequest() );

            var exception = Assert.ThrowsAsync<DefinedErrorException>( () => command.Response );
            var fake = feature.Items.OfType<FeatureDefinedExecutionError>().Single( e => e.Identifier == "Fake" );
            Assert.That( exception.ErrorIdentifier, Is.EqualTo( feature.GetFullyQualifiedIdentifier( fake ) ) );

            Unlock( lockId, token );
            Logout( token );
        }


        [Test]
        public void UnobservableCommand_RunsIntoUndefinedException_TranslatedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerClient( executionManager, _server.Channel );

            var exception = Assert.Throws<SilaException>( () => testClient.ThrowRuntimeException() );
            Assert.That(exception.Message, Contains.Substring(nameof(InvalidOperationException)));

            Unlock( lockId, token );
            Logout( token );
        }


        [Test]
        public void UnobservableCommandDynamic_RunsIntoUndefinedException_TranslatedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );

            var feature = _server.Features.Single( f => f.Identifier == "TestServer" );
            var featureContext = new FeatureContext( feature, _server, executionManager );
            var commandClient = new NonObservableCommandClient( feature.Items.OfType<FeatureCommand>().Single( c => c.Identifier == nameof( ITestServer.ThrowRuntimeException ) ), featureContext );
            var command = commandClient.InvokeAsync( commandClient.CreateRequest() );

            var exception = Assert.ThrowsAsync<SilaException>( () => command );
            Assert.That( exception.Message, Contains.Substring( nameof(InvalidOperationException) ) );

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
            Assert.Throws<DefinedErrorException>( () => Lock( null ) );
            Assert.Throws<DefinedErrorException>( () => Lock( "bullshit" ) );
        }

        private string Login()
        {
            var authenticationClient = new AuthenticationServiceClient(new DiscoveryExecutionManager(), _server.Channel );
            var token = authenticationClient.Login( "admin", "admin", _server.Config.Uuid.ToString(), new List<string>() );
            return token.AccessToken;
        }

        private string Lock( string token )
        {
            var lockingClient = new LockControllerClient(new TestAuthLockingExecutionManager(null, token, _server.Channel), _server.Channel );
            var lockIdentifier = Guid.NewGuid().ToString();
            lockingClient.LockServer(lockIdentifier, 3600);
            return lockIdentifier;
        }

        private void Unlock( string identifier, string token )
        {
            var lockingClient = new LockControllerClient( new TestAuthLockingExecutionManager( null, token, _server.Channel ), _server.Channel );
            lockingClient.UnlockServer(identifier);
        }

        private void Logout( string token )
        {
            var authenticationClient = new AuthenticationServiceClient( new DiscoveryExecutionManager(), _server.Channel );
            authenticationClient.Logout( token );
        }
    }
}
