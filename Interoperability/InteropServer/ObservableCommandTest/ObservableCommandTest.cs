using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Tecan.Sila2.Interop.Server
{
    [Export(typeof(IObservableCommandTest))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ObservableCommandTest : IObservableCommandTest
    {
        public IIntermediateObservableCommand<long, long> ObservableIteration( long numberIterations )
        {
            return ObservableCommands.Create<long, long>( ( _, sendIntermediate ) => CountToAsync( numberIterations, sendIntermediate ) );
        }

        public IObservableCommand<long> EchoValueAsync( long value, long delayInMs )
        {
            return ObservableCommands.Create( _ => SendValueAsnyc( value, delayInMs ) );
        }

        private async Task<long> SendValueAsnyc( long value, long delayInMs )
        {
            await Task.Delay( TimeSpan.FromMilliseconds( delayInMs ) );
            return value;
        }

        private async Task<long> CountToAsync( long numberOfIterations, Action<long> sendIntermediate )
        {
            for( int i = 0; i < numberOfIterations; i++ )
            {
                await Task.Delay( 100 );
                sendIntermediate( i );
            }

            await Task.Delay( 100 );
            return numberOfIterations;
        }
    }
}
