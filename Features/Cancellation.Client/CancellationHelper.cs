using System;
using System.Threading;
using Tecan.Sila2.Cancellation.CancelController;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Cancellation
{
    /// <summary>
    /// Denotes a helper class for cancellation of commands
    /// </summary>
    public static class CancellationHelper
    {
        /// <summary>
        /// Cancels the given command
        /// </summary>
        /// <param name="command">The command that should be cancelled</param>
        /// <param name="executionManager">The execution manager managing calls</param>
        public static void Cancel( IClientCommand command, IClientExecutionManager executionManager )
        {
            var client = new CancelControllerClient( command.CallInvoker, executionManager );
            client.CancelCommand(command.CommandId);
        }

        /// <summary>
        /// Registers the cancellation of the given command with the provided cancellation token
        /// </summary>
        /// <param name="command">The command that should be cancelled</param>
        /// <param name="executionManager">The execution manager managing calls</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void RegisterCancellation( IClientCommand command, IClientExecutionManager executionManager, CancellationToken cancellationToken )
        {
            if( cancellationToken.CanBeCanceled )
            {
                cancellationToken.Register( () => Cancel( command, executionManager ) );
            }
        }
    }
}
