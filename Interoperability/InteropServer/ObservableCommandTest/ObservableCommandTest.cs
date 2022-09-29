using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Tecan.Sila2.Interop.Server.ObservableCommandTest
{
    [Export( typeof( IObservableCommandTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class ObservableCommandTest : IObservableCommandTest
    {
        [return: SilaIdentifier( "IterationResponse" )]
        public IIntermediateObservableCommand<long, long> Count( long n, [Unit( "s", Second = 1 )] double delay )
        {
            return ObservableCommands.Create( ( Action<double, TimeSpan, CommandState> stateUpdate, Action<long> intermediate ) => CountAsync( n, TimeSpan.FromSeconds( delay ), stateUpdate, intermediate ) );
        }

        private async Task<long> CountAsync( long n, TimeSpan delayBetweenSteps, Action<double, TimeSpan, CommandState> update, Action<long> intermediateResponse )
        {
            for(int i = 0; i < n; i++)
            {
                await Task.Delay( delayBetweenSteps );
                update( (i + 1.0) / n, (n - i) * delayBetweenSteps, CommandState.Running );
                intermediateResponse( i );
            }
            return n - 1;
        }

        [return: SilaIdentifier( "ReceivedValue" )]
        public IObservableCommand<long> EchoValueAfterDelay( long value, [Unit( "s", Second = 1 )] double delay )
        {
            return ObservableCommands.Create( ( updates ) => EchoValueAfterDelay( updates, TimeSpan.FromSeconds( delay ), value ) );
        }

        private async Task<long> EchoValueAfterDelay( Action<double, TimeSpan, CommandState> updates, TimeSpan delay, long value )
        {
            updates( 0, delay, CommandState.NotStarted );
            await Task.Delay( delay );
            updates( 0, TimeSpan.Zero, CommandState.Running );
            return value;
        }
    }
}
