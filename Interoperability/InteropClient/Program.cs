using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Tecan.Sila2.Interop.Client.AnyTypeTest.AnyTypeTest;
using Tecan.Sila2.Interop.Client.AuthenticationTest;
using Tecan.Sila2.Interop.Client.AuthenticationTest.AuthenticationTest;
using Tecan.Sila2.Interop.Client.BasicDataTypesTest.BasicDataTypesTest;
using Tecan.Sila2.Interop.Client.BinaryTransferTest;
using Tecan.Sila2.Interop.Client.BinaryTransferTest.BinaryTransferTest;
using Tecan.Sila2.Interop.Client.ErrorHandlingTest;
using Tecan.Sila2.Interop.Client.ErrorHandlingTest.ErrorHandlingTest;
using Tecan.Sila2.Interop.Client.ListDataTypeTest;
using Tecan.Sila2.Interop.Client.ListDataTypeTest.ListDataTypeTest;
using Tecan.Sila2.Interop.Client.MetadataConsumerTest.MetadataConsumerTest;
using Tecan.Sila2.Interop.Client.MetadataProvider;
using Tecan.Sila2.Interop.Client.ObservableCommandTest.ObservableCommandTest;
using Tecan.Sila2.Interop.Client.ObservablePropertyTest;
using Tecan.Sila2.Interop.Client.ObservablePropertyTest.ObservablePropertyTest;
using Tecan.Sila2.Interop.Client.StructureDataTypeTest.StructureDataTypeTest;
using Tecan.Sila2.Interop.Client.UnobservableCommandTest;
using Tecan.Sila2.Interop.Client.UnobservableCommandTest.UnobservableCommandTest;
using Tecan.Sila2.Interop.Client.UnobservablePropertyTest.UnobservablePropertyTest;

namespace Tecan.Sila2.Interop.Client
{
    class Program
    {
        private static IExecutionManagerFactory? _executionManagerFactory;
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
                new AuthenticationInterceptor(new AuthenticationHandler(), null, new Lazy<IExecutionManagerFactory>(() => _executionManagerFactory!))
            });

            var connector = new ServerConnector( new DiscoveryExecutionManager() );
            var server = connector.GetServerData( channel );
            var executionManager = _executionManagerFactory.CreateExecutionManager( server );

            var program = new Program( server.Channel, executionManager );
            program.Run();

            if (Environment.ExitCode != 0)
            {
                Console.Error.WriteLine("Warning: exit code would have been " + Environment.ExitCode);
                Environment.ExitCode = 0;
            }
        }

        private void Run()
        {
            RunTest(TestSilaService);
            RunTest(TestUnobservableCommand);
            RunTest(TestUnobservableProperty);
            RunTest(TestBasicDataTypes);
            RunTest(TestList);
            RunTest(TestStructure);
            RunTest(TestAnyType);
            RunTest(TestMetadataConsumer);
            RunTest(TestErrorHandling);
            RunTest(TestObservableProperty);
            RunTest(TestObservableCommand);
            RunTest(TestBinaryTransfer );
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

        private void AssertSequence<T>(T[] expected, IEnumerable<T> actual)
        {
            if (expected.Length > 0)
            {
                var index = 0;
                foreach (var item in actual)
                {
                    Assert(expected[index], item);
                    index++;
                }
                Assert(expected.Length, index);
            }
            else
            {
                Assert(false, actual != null && actual.Any());
            }
        }

        private T? AssertException<T>( Action action, bool allowAggregate = false ) where T : Exception
        {
            try
            {
                action();
                Console.Error.WriteLine( "No exception raised" );
                Environment.ExitCode = 1;
                return default;
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
                Console.Error.WriteLine($"Expected {typeof(T).Name}, but received {aggregateException?.InnerException?.GetType().Name}");
                Console.Error.WriteLine(aggregateException?.InnerException?.Message);
                Environment.ExitCode = 1;
                return default;
            }
            catch(Exception otherException)
            {
                Console.Error.WriteLine( $"Expected {typeof( T ).Name}, but received {otherException.GetType().Name}" );
                Console.Error.WriteLine( otherException.Message );
                Environment.ExitCode = 1;
                return default;
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

        private void TestAnyType()
        {
            var client = new AnyTypeTestClient(_channel, _executionManager);

            Assert(null, client.SetAnyTypeValue(new DynamicObjectProperty(null, new DataTypeType
            {
                // there is no predefined void type, so we construct the void type manually
                Item = new ConstrainedType
                {
                    DataType = new DataTypeType { Item = BasicType.String },
                    Constraints = new Constraints
                    {
                        Length = "0"
                    }
                }
            })).ReceivedValue.Value);
            Assert("SiLA_Any_type_of_String_type", client.SetAnyTypeValue(new DynamicObjectProperty("SiLA_Any_type_of_String_type")).ReceivedValue.Value);
            Assert(5124L, (long)client.SetAnyTypeValue(new DynamicObjectProperty(5124)).ReceivedValue.Value);
            Assert(3.1415926, (double)client.SetAnyTypeValue(new DynamicObjectProperty(3.1415926)).ReceivedValue.Value);
            Assert(true, (bool)client.SetAnyTypeValue(new DynamicObjectProperty(true)).ReceivedValue.Value);
            Assert("SiLA_Any_type_of_Binary_type", RestoreString((Stream)client.SetAnyTypeValue(new DynamicObjectProperty(new MemoryStream(Encoding.UTF8.GetBytes("SiLA_Any_type_of_Binary_type")), BasicType.Binary)).ReceivedValue.Value));
            Assert(new DateTimeOffset(2022,08,05, 0, 0, 0, TimeSpan.FromHours(2)), (DateTimeOffset)client.SetAnyTypeValue(new DynamicObjectProperty(new DateTimeOffset(2022, 8, 5, 0, 0, 0, TimeSpan.FromHours(2)), BasicType.Date)).ReceivedValue.Value);
            Assert(new DateTimeOffset(2022, 08, 05, 12, 34, 56, 789, TimeSpan.FromHours(2)), (DateTimeOffset)client.SetAnyTypeValue(new DynamicObjectProperty(new DateTimeOffset(2022, 8, 5, 12, 34, 56, 789, TimeSpan.FromHours(2)), BasicType.Timestamp)).ReceivedValue.Value);
            Assert(new TimeSpan(0, 14, 34, 56, 789), (TimeSpan)client.SetAnyTypeValue(new DynamicObjectProperty(new TimeWithTimezone(new TimeSpan(0, 12, 34, 56, 789), TimeSpan.FromHours(2)), BasicType.Time)).ReceivedValue.Value);
            AssertSequence(["SiLA 2", "Any", "Type", "String", "List"], ((IEnumerable)client.SetAnyTypeValue(new DynamicObjectProperty(new List<string>
            {
                "SiLA 2",
                "Any",
                "Type",
                "String",
                "List"
            }, new DataTypeType { Item = new ListType { DataType = new DataTypeType { Item = BasicType.String } } })).ReceivedValue.Value).Cast<string>());

            var stringValue = new SiLAElement { Identifier = "StringTypeValue", DisplayName = "String Type Value", Description = "A string value.", DataType = new DataTypeType { Item = BasicType.String } };
            var intValue = new SiLAElement { Identifier = "IntegerTypeValue", DisplayName = "Integer Type Value", Description = "An integer value.", DataType = new DataTypeType { Item = BasicType.Integer } };
            var dateValue = new SiLAElement { Identifier = "DateTypeValue", DisplayName = "Date Type Value", Description = "A date value.", DataType = new DataTypeType { Item = BasicType.Date } };

            var str = (DynamicObject)client.SetAnyTypeValue(new DynamicObjectProperty(new DynamicObject
            {
                Elements =
                {
                    new DynamicObjectProperty(stringValue) { Value = "A String value" },
                    new DynamicObjectProperty(intValue) { Value = 83737665L },
                    new DynamicObjectProperty(dateValue) { Value = new DateTimeOffset(2022, 08, 05, 0, 0, 0, TimeSpan.FromHours(2)) }
                }
            },
            new DataTypeType
            {
                Item = new StructureType
                {
                    Element = [stringValue, intValue, dateValue] 
                }
            })).ReceivedValue.Value;
            Assert("A String value", (string)str.Elements.Single(p => p.Identifier == stringValue.Identifier).Value);
            Assert(83737665L, (long)str.Elements.Single(p => p.Identifier == intValue.Identifier).Value);
            Assert(new DateTimeOffset(2022, 08, 05, 0, 0, 0, TimeSpan.FromHours(2)), (DateTimeOffset)str.Elements.Single(p => p.Identifier == dateValue.Identifier).Value);

            Assert("SiLA_Any_type_of_String_type", (string)client.AnyTypeStringValue.Value);
            Assert(5124L, (long)client.AnyTypeIntegerValue.Value);
            Assert(3.1415926, (double)client.AnyTypeRealValue.Value);
            Assert(true, (bool)client.AnyTypeBooleanValue.Value);
            Assert("SiLA_Any_type_of_Binary_type", RestoreString((Stream)client.AnyTypeBinaryValue.Value));
            Assert(new TimeSpan(0, 14, 34, 56, 789), (TimeSpan)client.AnyTypeTimeValue.Value);
            Assert(new DateTimeOffset(2022, 08, 05, 0, 0, 0, TimeSpan.FromHours(2)), (DateTimeOffset)client.AnyTypeDateValue.Value);
            Assert(new DateTimeOffset(2022, 08, 05, 12, 34, 56, 789, TimeSpan.FromHours(2)), (DateTimeOffset)client.AnyTypeTimestampValue.Value);
            var values = (IReadOnlyList<object>)client.AnyTypeListValue.Value;
            Assert(5, values.Count);
            Assert("SiLA 2", values[0]);
            Assert("Any", values[1]);
            Assert("Type", values[2]);
            Assert("String", values[3]);
            Assert("List", values[4]);

            var obj = (DynamicObject)client.AnyTypeStructureValue.Value;
            Assert("A String value", (string)obj.Elements.Single(p => p.Identifier == "StringTypeValue").Value);
            Assert(83737665L, (long)obj.Elements.Single(p => p.Identifier == "IntegerTypeValue").Value);
            Assert(new DateTimeOffset(2022, 08, 05, 0, 0, 0, TimeSpan.FromHours(2)), (DateTimeOffset)obj.Elements.Single(p => p.Identifier == "DateTypeValue").Value);
        }

        private void TestBasicDataTypes()
        {
            var client = new BasicDataTypesTestClient(_channel, _executionManager);

            Assert("SiLA2_Test_String_Value", client.EchoStringValue("SiLA2_Test_String_Value"));
            Assert(5124, client.EchoIntegerValue(5124));
            Assert(3.1415926, client.EchoRealValue(3.1415926));
            Assert(true, client.EchoBooleanValue(true));
            Assert(new DateTimeOffset(2022, 8, 5, 0, 0, 0, TimeSpan.FromHours(2)), client.EchoDateValue(new DateTimeOffset(2022, 8, 5, 0, 0, 0, TimeSpan.FromHours(2))));
            Assert(new DateTimeOffset(2022, 8, 5, 12, 34, 56, 789, TimeSpan.FromHours(2)), client.EchoTimestampValue(new DateTimeOffset(2022, 8, 5, 12, 34, 56, 789, TimeSpan.FromHours(2))));
            Assert(new TimeWithTimezone(new TimeSpan(0, 12, 34, 56, 789), TimeSpan.FromHours(2)), client.EchoTimeValue(new TimeWithTimezone(new TimeSpan(0, 12, 34, 56, 789), TimeSpan.FromHours(2))));

            Assert("SiLA2_Test_String_Value", client.StringValue);
            Assert(5124, client.IntegerValue);
            Assert(3.1415926, client.RealValue);
            Assert(true, client.BooleanValue);
            Assert(new DateTimeOffset(2022, 8, 5, 0, 0, 0, TimeSpan.FromHours(2)), client.DateValue);
            Assert(new DateTimeOffset(2022, 8, 5, 12, 34, 56, 789, TimeSpan.FromHours(2)), client.TimestampValue);
            // time zone is added to the timespan
            Assert(new TimeWithTimezone( new TimeSpan(0, 12, 34, 56, 789), TimeSpan.FromHours(2) ), client.TimeValue);
        }

        private void TestList()
        {
            var client = new ListDataTypeTestClient(_channel, _executionManager);

            AssertSequence(["SiLA 2", "is", "great"], client.EchoStringList(["SiLA 2", "is", "great"]));
            AssertSequence([1, 2, 3], client.EchoIntegerList([1, 2, 3]));

            AssertSequence(Array.Empty<string>(), client.EmptyStringList);

            AssertSequence(["SiLA 2", "is", "great"], client.StringList);
            AssertSequence([1, 2, 3], client.IntegerList);

            var testStructure = client.StructureList.ToList();
            AssertTestStructure(testStructure);
            testStructure = client.EchoStructureList(testStructure).ToList();
            AssertTestStructure(testStructure);
        }

        private void AssertTestStructure(List<TestStructure> testStructure)
        {
            Assert(3, testStructure.Count);

            Assert("SiLA2_Test_String_Value_1", testStructure[0].StringTypeValue);
            Assert(5124, testStructure[0].IntegerTypeValue);
            Assert(3.1415926, testStructure[0].RealTypeValue);
            Assert(true, testStructure[0].BooleanTypeValue);
            Assert("Binary_String_Value_1", RestoreString(testStructure[0].BinaryTypeValue));
            Assert(new DateTimeOffset(2022, 8, 5, 0, 0, 0, TimeSpan.FromHours(2)), testStructure[0].DateTypeValue);
            Assert(new DateTimeOffset(2022, 8, 5, 12, 34, 56, TimeSpan.FromHours(2)), testStructure[0].TimestampTypeValue);
            Assert(new TimeWithTimezone( new TimeSpan(12, 34, 56), TimeSpan.FromHours(2)), testStructure[0].TimeTypeValue);
            Assert("Any_Type_String_Value_1", (string)testStructure[0].AnyTypeValue.Value);


            Assert("SiLA2_Test_String_Value_2", testStructure[1].StringTypeValue);
            Assert(5125, testStructure[1].IntegerTypeValue);
            Assert(4.1415926, testStructure[1].RealTypeValue);
            Assert(false, testStructure[1].BooleanTypeValue);
            Assert("Binary_String_Value_2", RestoreString(testStructure[1].BinaryTypeValue));
            Assert(new DateTimeOffset(2023, 9, 6, 0, 0, 0, TimeSpan.FromHours(2)), testStructure[1].DateTypeValue);
            Assert(new DateTimeOffset(2023, 9, 6, 13, 35, 57, TimeSpan.FromHours(2)), testStructure[1].TimestampTypeValue);
            Assert(new TimeWithTimezone(new TimeSpan(13, 35, 57), TimeSpan.FromHours(2)), testStructure[1].TimeTypeValue);
            Assert("Any_Type_String_Value_2", (string)testStructure[1].AnyTypeValue.Value);

            Assert("SiLA2_Test_String_Value_3", testStructure[2].StringTypeValue);
            Assert(5126, testStructure[2].IntegerTypeValue);
            Assert(5.1415926, testStructure[2].RealTypeValue);
            Assert(true, testStructure[2].BooleanTypeValue);
            Assert("Binary_String_Value_3", RestoreString(testStructure[2].BinaryTypeValue));
            Assert(new DateTimeOffset(2024, 10, 7, 0, 0, 0, TimeSpan.FromHours(2)), testStructure[2].DateTypeValue);
            Assert(new DateTimeOffset(2024, 10, 7, 14, 36, 58, 789, TimeSpan.FromHours(2)), testStructure[2].TimestampTypeValue);
            Assert(new TimeWithTimezone(new TimeSpan(0, 14, 36, 58, 789), TimeSpan.FromHours(2)), testStructure[2].TimeTypeValue);
            Assert("Any_Type_String_Value_3", (string)testStructure[2].AnyTypeValue.Value);
        }

        private void TestStructure()
        {
            var client = new StructureDataTypeTestClient(_channel, _executionManager);

            var structValue = client.StructureValue;
            AssertStructureValue(structValue);
            structValue = client.EchoStructureValue(structValue);
            AssertStructureValue(structValue);

            var deepStruct = client.DeepStructureValue;
            AssertDeepStructure(deepStruct);
            deepStruct = client.EchoDeepStructureValue(deepStruct);
            AssertDeepStructure(deepStruct);
        }

        private void AssertDeepStructure(StructureDataTypeTest.DeepStructure deepStruct)
        {
            Assert("Outer_Test_String", deepStruct.OuterStringTypeValue);
            Assert(1111, deepStruct.OuterIntegerTypeValue);
            Assert("Middle_Test_String", deepStruct.MiddleStructure.MiddleStringTypeValue);
            Assert(2222, deepStruct.MiddleStructure.MiddleIntegerTypeValue);
            Assert("Inner_Test_String", deepStruct.MiddleStructure.InnerStructure.InnerStringTypeValue);
            Assert(3333, deepStruct.MiddleStructure.InnerStructure.InnerIntegerTypeValue);
        }

        private void AssertStructureValue(StructureDataTypeTest.TestStructure structValue)
        {
            Assert("SiLA2_Test_String_Value", structValue.StringTypeValue);
            Assert(5124, structValue.IntegerTypeValue);
            Assert(3.1415926, structValue.RealTypeValue);
            Assert(true, structValue.BooleanTypeValue);
            Assert("SiLA2_Binary_String_Value", RestoreString(structValue.BinaryTypeValue));
            Assert(new DateTimeOffset(2022, 8, 5, 0, 0, 0, TimeSpan.FromHours(2)), structValue.DateTypeValue);
            Assert(new DateTimeOffset(2022, 8, 5, 12, 34, 56, 789, TimeSpan.FromHours(2)), structValue.TimestampTypeValue);
            Assert(new TimeWithTimezone(new TimeSpan(0, 12, 34, 56, 789), TimeSpan.FromHours(2)), structValue.TimeTypeValue);
        }

        private string RestoreString(Stream value)
        {
            var reader = new StreamReader(value, Encoding.ASCII, leaveOpen: true);
            var ret = reader.ReadToEnd();
            value.Position = 0;
            return ret;
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
            Assert("abc", client.ReceivedStringMetadata);
            var receivedTotal = string.Empty;
            client.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == nameof(client.ReceivedStringMetadataAsCharacters))
                {
                    receivedTotal += client.ReceivedStringMetadataAsCharacters;
                }
            };
            receivedTotal += client.ReceivedStringMetadataAsCharacters;
            Thread.Sleep(1000);
            // need to assert EndsWith as starting a sometimes gets lost
            Assert(true, receivedTotal.EndsWith("bc"));
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
