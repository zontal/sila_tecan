using System;
using System.ComponentModel.Composition;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Cancellation
{
    /// <summary>
    /// Denotes the default implementation for a cancellation service
    /// </summary>
    [Export(typeof(ICancelController))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class CancelControllerService : ICancelController
    {
        private readonly ISiLAServer _server;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        [ImportingConstructor]
        public CancelControllerService( ISiLAServer server )
        {
            _server = server;
        }

        /// <inheritdoc />
        public void CancelCommand( Uuid commandExecutionUuid )
        {
            IObservableCommand command;
            try
            {
                command = _server.GetCommandExecution( commandExecutionUuid.Value ).Command;
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

        /// <inheritdoc />
        public void CancelAll()
        {
            foreach( var commandExecution in _server.FindCommandExecutions(state => state == CommandState.Running || state == CommandState.NotStarted) )
            {
                if( commandExecution.Command.IsCancellationSupported )
                {
                    commandExecution.Command.Cancel();
                }
            }
        }
    }
}
