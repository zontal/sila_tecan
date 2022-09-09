using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Recovery
{
    /// <summary>
    /// Denotes an abstract interface for a service to recover from exceptions
    /// </summary>
    public interface IRecoveryService
    {
        /// <summary>
        /// Recovers from the given exception
        /// </summary>
        /// <param name="exception">The exception from which the command should recover</param>
        /// <param name="context">The context in which the exception has happened</param>
        /// <param name="duration">The duration until the recovery is failing</param>
        /// <param name="attempt">The 0-indexed attempt</param>
        /// <returns>A task that returns a result of the recovery</returns>
        Task<RecoveryResult> Recover( Exception exception, IReadOnlyDictionary<string, string> context, TimeSpan? duration, int attempt );
    }
}
