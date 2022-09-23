using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Recovery
{
    /// <summary>
    /// Denotes a recovery result
    /// </summary>
    public enum RecoveryResult
    {
        /// <summary>
        /// The recovery has recovered the state before the command execution, execution can continue but the command should be retried
        /// </summary>
        Retry,

        /// <summary>
        /// The recovery succeeded but the process should be aborted
        /// </summary>
        Abort,

        /// <summary>
        /// The recovery failed and the original exception should be rethrown
        /// </summary>
        Throw,

        /// <summary>
        /// The recovery succeeded and the process can continue
        /// </summary>
        Continue
    }
}
