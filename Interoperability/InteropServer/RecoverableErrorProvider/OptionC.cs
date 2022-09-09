using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Tecan.Sila2.Recovery;

namespace Tecan.Sila2.Interop.Server.RecoverableErrorProvider
{
    [Export( typeof( IRecoveryStrategy ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class OptionC : IRecoveryStrategy
    {
        public string Identifier => nameof( OptionC );

        public string Description => "Recover the error by choosing option A";

        public Type ParameterType => null;

        public int Priority => 0;

        public bool CanRecover( Exception exception, IReadOnlyDictionary<string, string> context, int attempt )
        {
            return true;
        }

        public RecoveryResult Recover( Exception exception, IReadOnlyDictionary<string, string> context, object parameter )
        {
            RecoverableErrorProviderTest.ChosenRecovery = Identifier;
            return RecoveryResult.Continue;
        }
    }
}
