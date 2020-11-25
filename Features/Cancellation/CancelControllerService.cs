using System;
using System.ComponentModel.Composition;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Cancellation
{
    [Export(typeof(ICancelController))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CancelControllerService : ICancelController
    {
        private readonly ISiLAServer _server;

        [ImportingConstructor]
        public CancelControllerService( ISiLAServer server )
        {
            _server = server;
        }

        public void CancelCommand( Uuid commandExecutionUuid )
        {
            IObservableCommand command;
            try
            {
                command = _server.GetCommandExecution( commandExecutionUuid.Value );
            }
            catch( Exception ex )
            {
                throw new InvalidCommandExecutionUUIDException( ex.Message );
            }

            if( command.IsCancellationSupported )
            {
                command.Cancel();
            }
            else
            {
                throw new OperationNotSupportedException( "The provided command does not support cancellation." );
            }
        }

        public void CancelAll()
        {
            foreach( var commandExecution in _server.FindCommandExecutions(state => state == CommandState.Running || state == CommandState.NotStarted) )
            {
                if( commandExecution.IsCancellationSupported )
                {
                    commandExecution.Cancel();
                }
            }
        }
    }
}
