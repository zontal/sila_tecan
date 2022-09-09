using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Recovery
{
    internal class RecoverableErrorInternal
    {
        private readonly TaskCompletionSource<RecoveryResult> _completionSource = new TaskCompletionSource<RecoveryResult>();

        public RecoverableErrorInternal( List<IRecoveryStrategy> strategies, string defaultOption, DateTime? expiry, ObservableCommandExecution commandExecution, IReadOnlyDictionary<string, string> context, Exception exception )
        {
            Strategies = strategies;
            DefaultOption = defaultOption;
            Expiry = expiry;
            Command = commandExecution;
            Context = context;
            Exception = exception;
        }

        public List<IRecoveryStrategy> Strategies { get; }

        public string DefaultOption { get; }

        public DateTime? Expiry { get; set; }

        public IReadOnlyDictionary<string, string> Context { get; }

        public ObservableCommandExecution Command { get; }

        public Exception Exception { get; }

        public Task<RecoveryResult> RecoveryTask => _completionSource.Task;

        internal void Abort()
        {
            _completionSource.SetResult( RecoveryResult.Throw );
        }

        public RecoverableError AsRecoverableError()
        {
            return new RecoverableError( Command.CommandIdentifier, Command.CommandExecutionID, Exception.Message, Strategies.Select( AsContinuationOption ).ToList(), DefaultOption, GetExpiry() );
        }

        private Timeout GetExpiry()
        {
            if(Expiry.HasValue)
            {
                return new Timeout( (long)(Expiry.Value - DateTime.Now).TotalSeconds );
            }
            return new Timeout( 0 );
        }

        internal void Recover( string continuationOption, object value )
        {
            var strategy = Strategies.FirstOrDefault( s => s.Identifier == continuationOption );
            if(strategy == null)
            {
                throw new UnknownContinuationOptionException( $"Continuation option {continuationOption} is not known" );
            }
            var result = strategy.Recover( Exception, Context, strategy.ParameterType == null ? null : value );
            _completionSource.SetResult( result );
        }

        private ContinuationOption AsContinuationOption( IRecoveryStrategy strategy )
        {
            return new ContinuationOption( strategy.Identifier, strategy.Description, null );
        }


    }
}
