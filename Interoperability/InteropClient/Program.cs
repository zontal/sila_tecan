using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using NUnit.Framework;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.Interop.Client
{
    class Program
    {
        private readonly IClientExecutionManager _executionManager;
        private readonly IClientChannel _channel;

        static void Main( string[] args )
        {
            var serverId = Environment.GetEnvironmentVariable( "ServerID" );
            var port = Environment.GetEnvironmentVariable( "Port" ) ?? "50052";

            var channel = new Channel( "localhost", int.Parse( port ), ChannelCredentials.Insecure );
            var executionManager = new ExecutionManager( channel );

            var connector = new ServerConnector( executionManager );
            var server = connector.GetServerData( channel );

            var program = new Program( server.Channel, executionManager );
            program.RunTest( Environment.GetEnvironmentVariable( "Server" ), Environment.GetEnvironmentVariable( "Test" ) );
        }

        public Program( IClientChannel channel, IClientExecutionManager executionManager )
        {
            _channel = channel;
            _executionManager = executionManager;
        }

        public void RunTest( string server, string test )
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                switch(test)
                {
                    default:
                        throw new InvalidOperationException( $"Test {test} is not implemented." );
                }
                stopwatch.Stop();
                Console.WriteLine( $"{server};{Environment.GetEnvironmentVariable( "Client" )};{test};pass;{(long)stopwatch.Elapsed.TotalMilliseconds}" );
            }
            catch(Exception e)
            {
                Console.WriteLine( $"{server};{Environment.GetEnvironmentVariable( "Client" )};{test};fail;{(long)stopwatch.Elapsed.TotalMilliseconds}" );
                Console.Error.WriteLine( $"Execution of test {test} ran into an exception: {e.Message}" );
            }

        }
    }
}
