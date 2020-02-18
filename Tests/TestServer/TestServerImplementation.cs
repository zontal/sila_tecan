using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tecan.Sila2.IntegrationTests
{
    [Export(typeof(ITestServer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class TestServerImplementation : ITestServer
    {
        public bool PassBool( bool data )
        {
            return data;
        }

        public double PassDouble( double data )
        {
            return data;
        }

        public long PassLong( long data )
        {
            return data;
        }

        public int PassInt( int data )
        {
            return data;
        }

        public DateTimeOffset PassTimestamp( DateTimeOffset timestamp )
        {
            return timestamp;
        }

        public TimeSpan PassTime( TimeSpan time )
        {
            return time;
        }

        public string PassString( string value )
        {
            return value;
        }

        public Stream PassStream( Stream value )
        {
            return value;
        }

        public FileInfo PassFile( FileInfo file )
        {
            return file;
        }

        public byte[] PassBytes( byte[] bytes )
        {
            return bytes;
        }

        public SomeStructure PassStructure( SomeStructure structure )
        {
            return structure;
        }

        public ICollection<string> PassCollection( ICollection<string> strings )
        {
            return strings;
        }

        public IObservableCommand<int> RunObservable()
        {
            return ObservableCommands.Create( RunObservableCore );
        }

        private async Task<int> RunObservableCore( Action<double, TimeSpan, CommandState> updateState)
        {
            updateState( 0, TimeSpan.FromHours( 2 ), CommandState.NotStarted );
            await Task.Delay( 100 );
            updateState( 0.8, TimeSpan.FromSeconds( 23 ), CommandState.Running );
            await Task.Delay( 100 );
            updateState( 0.7, TimeSpan.FromSeconds( 42 ), CommandState.Running );
            await Task.Delay( 100 );
            return 42;
        }

        public IIntermediateObservableCommand<int> RunIntermediateObservableCommand()
        {
            return ObservableCommands.Create<int>( RunIntermediateObservableCommandCore );
        }


        private async Task RunIntermediateObservableCommandCore( Action<double, TimeSpan, CommandState> updateState, Action<int> sendIntermediateResult)
        {
            sendIntermediateResult( 0 );
            await Task.Delay( 100 );
            sendIntermediateResult( 8 );
            await Task.Delay( 100 );
            sendIntermediateResult( 15 );
        }

        public async Task WaitForCancelOrThrow( TimeSpan duration, CancellationToken cancellationToken )
        {
            await Task.Delay( duration, cancellationToken );
            throw new ShouldHaveBeenCancelledException("The command was supposed to be cancelled");
        }

        public void RunIntoFailure()
        {
            throw new FakeException("This is only fake");
        }

        public void RunIntoFailureObservably( CancellationToken cancellationToken )
        {
            throw new FakeException("This is only fake");
        }
    }
}
