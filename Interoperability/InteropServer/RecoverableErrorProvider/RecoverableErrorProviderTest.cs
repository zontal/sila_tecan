using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Recovery;

namespace Tecan.Sila2.Interop.Server.RecoverableErrorProvider
{
    [Export( typeof( IRecoverableErrorProvider ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class RecoverableErrorProviderTest : IRecoverableErrorProvider
    {
        private readonly IRecoveryService _recoveryService;

        [ImportingConstructor]
        public RecoverableErrorProviderTest( IRecoveryService recoveryService )
        {
            _recoveryService = recoveryService;
        }

        public static string ChosenRecovery { get; set; }

        [return: SilaIdentifier( "SelectedContinuationOption" )]
        public IObservableCommand<string> RaiseRecoverableError( [MinimalInclusive( 0 ), Unit( "s", Second = 1 )] long durationUntilError, [MinimalInclusive( 0 ), Unit( "s", Second = 1 )] long automaticExecutionTimeout )
        {
            return new RecoverableErrorCommand( _recoveryService, TimeSpan.FromSeconds( durationUntilError ), TimeSpan.FromSeconds( automaticExecutionTimeout ) );
        }

        private class RecoverableErrorCommand : ObservableCommand<string>, IServerCommand
        {
            private readonly IRecoveryService _recoveryService;

            public RecoverableErrorCommand( IRecoveryService recoveryService, TimeSpan durationUntilError, TimeSpan timeoutForRecovery )
            {
                _recoveryService = recoveryService;
                DurationUntilError = durationUntilError;
                TimeoutForRecovery = timeoutForRecovery;
            }

            public TimeSpan DurationUntilError { get; }

            public TimeSpan TimeoutForRecovery { get; }

            public string CommandExecutionUuid { get; set; }

            public override async Task<string> Run()
            {
                await Task.Delay( DurationUntilError );
                var context = new Dictionary<string, string>();
                context.Add( nameof( CommandExecutionUuid ), CommandExecutionUuid );
                await _recoveryService.Recover( new Exception(), context, TimeoutForRecovery, 0 );
                return ChosenRecovery;
            }
        }
    }
}
