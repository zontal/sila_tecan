using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using NUnit.Framework;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.Interop.Client.DataTypeProvider;
using Tecan.Sila2.Interop.Client.ObservableCommandTest;
using Tecan.Sila2.Interop.Client.ParameterConstraintsTest;

namespace Tecan.Sila2.Interop.Client
{
    class Program
    {
        private readonly IClientExecutionManager _executionManager;
        private readonly IClientChannel _channel;

        static void Main(string[] args)
        {
            var serverId = Environment.GetEnvironmentVariable( "ServerID" );
            var port = Environment.GetEnvironmentVariable( "Port" );

            var channel = new Channel( "localhost", int.Parse( port ), ChannelCredentials.Insecure );
            var executionManager = new ExecutionManager(channel);
            var program = new Program(channel, executionManager);
            program.RunTest(Environment.GetEnvironmentVariable("Server"), Environment.GetEnvironmentVariable("Test"));
        }

        public Program( Channel channel, IClientExecutionManager executionManager )
        {
            _channel = null;
            _executionManager = executionManager;
        }

        public void RunTest(string server, string test)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                switch( test )
                {
                    case "datatypes":
                        TestDataTypeProvider();
                        break;
                    case "echo":
                        TestEchos();
                        break;
                    case "observableCommands":
                        TestObservableCommands().Wait();
                        break;
                    case "constraints":
                        TestConstraints();
                        break;
                    default:
                        throw new InvalidOperationException( $"Test {test} is not implemented." );
                }
                stopwatch.Stop();
                Console.WriteLine($"{server};{Environment.GetEnvironmentVariable("Client")};{test};pass;{(long)stopwatch.Elapsed.TotalMilliseconds}");
            }
            catch( Exception e )
            {
                Console.WriteLine($"{server};{Environment.GetEnvironmentVariable("Client")};{test};fail;{(long)stopwatch.Elapsed.TotalMilliseconds}");
                Console.Error.WriteLine($"Execution of test {test} ran into an exception: {e.Message}");
            }

        }

        private void TestConstraints()
        {
            var client = new ParameterConstraintsTestClient( _executionManager, _channel );

            var stringLen10 = new string( '=', 10 );
            var legalOption = "First option";
            var legalPatternString = "12/34/56/7890";
            var shortString = "1";
            var longString = "A test string that is more than 20 characters long.";
            Assert.DoesNotThrow( () => client.ExactStringLength( stringLen10, stringLen10, stringLen10, stringLen10, legalOption, legalPatternString ) );
            Assert.Throws<ValidationException>( () => client.ExactStringLength( shortString, stringLen10, stringLen10, stringLen10, legalOption, legalPatternString ) );
            Assert.Throws<ValidationException>( () => client.ExactStringLength( longString, stringLen10, stringLen10, stringLen10, legalOption, legalPatternString ) );
            Assert.Throws<ValidationException>( () => client.ExactStringLength( stringLen10, longString, stringLen10, stringLen10, legalOption, legalPatternString ) );
            Assert.Throws<ValidationException>( () => client.ExactStringLength( stringLen10, stringLen10, shortString, stringLen10, legalOption, legalPatternString ) );
            Assert.Throws<ValidationException>( () => client.ExactStringLength( stringLen10, stringLen10, stringLen10, shortString, legalOption, legalPatternString ) );
            Assert.Throws<ValidationException>( () => client.ExactStringLength( stringLen10, stringLen10, stringLen10,longString , legalOption, legalPatternString ) );
            Assert.Throws<ValidationException>( () => client.ExactStringLength( stringLen10, stringLen10, stringLen10, stringLen10, shortString, legalPatternString ) );
            Assert.Throws<ValidationException>( () => client.ExactStringLength( stringLen10, stringLen10, stringLen10, stringLen10, legalOption, longString ) );
        }

        private async Task TestObservableCommands()
        {
            var client = new ObservableCommandTestClient( _executionManager, _channel );
            var response = client.EchoValueAsync( 42, 100 ).Response;
            await response;
            Assert.That(response.Result, Is.EqualTo(42L));

            var next = 1;
            var counter = client.ObservableIteration( 20 );
            while( await counter.IntermediateValues.WaitToReadAsync() )
            {
                if( counter.IntermediateValues.TryRead( out var current ) )
                {
                    Assert.That(current, Is.EqualTo(next));
                    next++;
                }
            }
            Assert.That(next, Is.EqualTo(21));
            Assert.That(await counter.Response, Is.EqualTo(20L));
        }

        private void TestEchos()
        {
            var client = new DataTypeProviderClient();
            Assert.That( client.EchoBooleanValue( true ), Is.True );
            Assert.That( client.EchoBooleanValue(false), Is.False);

            Assert.That(client.EchoStringValue(""), Is.EqualTo(""));
            Assert.That( client.EchoStringValue( "Foo" ), Is.EqualTo( "Foo" ) );
            Assert.That( client.EchoStringValue( "Dürüm" ), Is.EqualTo( "Dürüm" ) );
            Assert.That( client.EchoStringValue( "20‰" ), Is.EqualTo( "20‰" ) );

            Assert.That(client.EchoIntegerValue(0), Is.EqualTo(0L));
            Assert.That( client.EchoIntegerValue( int.MaxValue * 4L ), Is.EqualTo( int.MaxValue * 4L ) );
            Assert.That( client.EchoIntegerValue( -42 ), Is.EqualTo( -42L ) );

            Assert.That(client.EchoRealValue(0.0), Is.EqualTo(0.0));
            Assert.That(client.EchoRealValue(double.NaN), Is.EqualTo(double.NaN));
            Assert.That(client.EchoRealValue(double.Epsilon), Is.GreaterThan(0.0));

            var stringList = new List<string>();
            Assert.That(client.EchoStringList(stringList), Is.EquivalentTo(stringList));
            stringList.Add("SiLA2");
            Assert.That( client.EchoStringList( stringList ), Is.EquivalentTo( stringList ) );
            stringList.Add("is");
            Assert.That( client.EchoStringList( stringList ), Is.EquivalentTo( stringList ) );
            stringList.Add("great.");
            Assert.That( client.EchoStringList( stringList ), Is.EquivalentTo( stringList ) );

            var intList = new List<long>();
            Assert.That(client.EchoIntegerList(intList), Is.EquivalentTo(intList));
            intList.Add(0);
            Assert.That( client.EchoIntegerList( intList ), Is.EquivalentTo( intList ) );
            intList.Add(8);
            Assert.That( client.EchoIntegerList( intList ), Is.EquivalentTo( intList ) );
            intList.Add(15);
            Assert.That( client.EchoIntegerList( intList ), Is.EquivalentTo( intList ) );
        }

        private void TestDataTypeProvider()
        {
            var client = new DataTypeProviderClient();

            Assert.That( client.StringValue, Is.EqualTo( "SiLA2_Test_String_Value" ) );
            Assert.That( client.IntegerValue, Is.EqualTo( 42L ) );
            Assert.That( client.RealValue, Is.EqualTo( 3.1415926 ).Within( 0.0001 ) );
            Assert.That( client.BooleanValue, Is.True );
            var buffer = new byte[1024];
            var bytesRead = client.BinaryValue.Read( buffer, 0, buffer.Length );
            Assert.That( Encoding.UTF8.GetString( buffer, 0, bytesRead ), Is.EqualTo( "SiLA2_Test_String_Value" ) );
            Assert.That( client.DateValue.Date, Is.EqualTo( new DateTime( 2019, 8, 24 ) ) );
            Assert.That( client.TimeValue, Is.EqualTo( new TimeSpan( 12, 34, 56 ) ) );
            Assert.That( client.TimeStampValue.DateTime, Is.EqualTo( new DateTime( 2018, 8, 24, 12, 34, 56 ) ) );
            Assert.That( client.StringList, Is.EquivalentTo( new string[] { "SiLA 2", "is", "great" } ) );
            Assert.That( client.IntegerList, Is.EquivalentTo( new long[] { 1, 2, 3 } ) );

            Assert.That( client.StructureValues.StringStructureElement, Is.EqualTo( "SiLA2_Test_String_Value" ) );
            Assert.That( client.StructureValues.IntegerStructureElement, Is.EqualTo( 42L ) );
            Assert.That( client.StructureValues.RealStructureElement, Is.EqualTo( 3.1415926 ).Within( 0.0001 ) );
            Assert.That( client.StructureValues.BooleanStructureElement, Is.True );
            Assert.That( client.StructureValues.DateStructureElement.DateTime, Is.EqualTo( new DateTime( 2019, 8, 24 ) ) );
            Assert.That( client.StructureValues.TimeStructureElement, Is.EqualTo( new TimeSpan( 12, 34, 56 ) ) );
            Assert.That( client.StructureValues.TimestampStructureElement.DateTime, Is.EqualTo( new DateTime( 2018, 8, 24, 12, 34, 56 ) ) );

            var mixedValues = client.GetMultipleMixedValues();
            Assert.That( mixedValues.StringValue, Is.EqualTo( "SiLA2_Test_String_Value" ) );
            Assert.That( mixedValues.IntegerValue, Is.EqualTo( 42L ) );
            Assert.That( mixedValues.RealValue, Is.EqualTo( 3.1415926 ).Within( 0.0001 ) );
            Assert.That( mixedValues.BooleanValue, Is.True );
            Assert.That( mixedValues.DateValue.Date, Is.EqualTo( new DateTime( 2019, 8, 24 ) ) );
            Assert.That( mixedValues.TimeValue, Is.EqualTo( new TimeSpan( 12, 34, 56 ) ) );
            Assert.That( mixedValues.TimeStampValue.DateTime, Is.EqualTo( new DateTime( 2018, 8, 24, 12, 34, 56 ) ) );
        }
    }
}
