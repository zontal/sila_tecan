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
                update( (i + 1.0) / n, new TimeSpan( (n - i) * delayBetweenSteps.Ticks), CommandState.Running );
                intermediateResponse( i );
            }
            return n - 1;
        }

        [return: SilaIdentifier( "ReceivedValue" )]
        public IObservableCommand<long> EchoValueAfterDelay( long value, [Unit( "s", Second = 1 )] double delay )
        {
            return new EchoValuesAfterDelayCommand( TimeSpan.FromSeconds( delay ), value );
        }

        private class EchoValuesAfterDelayCommand : ObservableCommand<long>
        {
            private readonly TimeSpan _delay;
            private readonly long _value;

            public EchoValuesAfterDelayCommand(TimeSpan delay, long value)
            {
                _delay = delay;
                _value = value;
            }   

            public override async Task<long> Run()
            {
                PushStateUpdate(0, _delay, CommandState.NotStarted);
                await Task.Delay(_delay);
                PushStateUpdate(0, TimeSpan.Zero, CommandState.Running);
                return _value;
            }
        }
    }
}
