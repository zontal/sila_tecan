using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.Cancellation.CancelController
{
    public partial class CancelControllerClient
    {
        ///  <summary>
        /// Cancel a specified currently running Observable Command or cancel all currently running Commands (Observable and
        /// Unobservable).
        /// For any canceled Observable Command the SiLA Server MUST update the Command Execution Status to "Command Finished
        /// with Error".
        /// The SiLA Server MUST throw a descriptive error message indicating cancellation as the reason for the Command
        /// execution not being able to finish successfully for any canceled Command.
        /// </summary>
        /// <param name="commandId">The Command Execution UUID according to the SiLA Standard.</param>
        public void CancelCommand( CommandExecutionUuid commandId )
        {
            CancelCommand(new Uuid(commandId.CommandId));
        }
    }
}
