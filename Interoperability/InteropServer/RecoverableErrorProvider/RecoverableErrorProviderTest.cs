using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace Tecan.Sila2.Interop.Server.RecoverableErrorProvider
{
    [Export( typeof( IRecoverableErrorProvider ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class RecoverableErrorProviderTest : IRecoverableErrorProvider
    {
        [return: SilaIdentifier( "SelectedContinuationOption" )]
        public IObservableCommand<string> RaiseRecoverableError( [MinimalInclusive( 0 ), Unit( "s", Second = 1 )] long durationUntilError, [MinimalInclusive( 0 ), Unit( "s", Second = 1 )] long automaticExecutionTimeout )
        {
            throw new NotImplementedException();
        }
    }
}
