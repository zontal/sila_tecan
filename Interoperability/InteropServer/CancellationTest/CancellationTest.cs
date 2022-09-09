using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tecan.Sila2.Interop.Server.CancellationTest
{
    [Export( typeof( ICancellationTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class CancellationTest : ICancellationTest
    {
        public IObservableCommand WaitForCancel( [Unit( "s", Second = 1 )] long duration )
        {
            return ObservableCommands.Create( token => WaitForCancel( TimeSpan.FromSeconds( duration ), token ) );
        }

        private async Task WaitForCancel( TimeSpan duration, CancellationToken cancellationToken )
        {
            await Task.Delay( duration, cancellationToken );
            throw new ShouldHaveBeenCancelledException( "The command was supposed to be cancelled" );
        }
    }
}
