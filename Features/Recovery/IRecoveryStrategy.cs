using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2;

namespace Tecan.Sila2.Recovery
{

    /// <summary>
    /// Denotes an abstract recovery strategy
    /// </summary>
    public interface IRecoveryStrategy
    {
        /// <summary>
        /// Gets the identifier of the recovery strategy
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Gets the description of the recovery strategy
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the priority of the recovery strategy that indicates whether the strategy should be suggested as default
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Determines whether the system is able to recover from the provided exception
        /// </summary>
        /// <param name="exception">The exception to recover from</param>
        /// <param name="context">The context in which the exception has occured</param>
        /// <param name="attempt">The number of the attempts</param>
        /// <returns>True, if the recovery supports to recover from the given exception, otherwise False</returns>
        bool CanRecover( Exception exception, IReadOnlyDictionary<string, string> context, int attempt );

        /// <summary>
        /// Gets the type of input parameter for this strategy or null, in case the strategy does not require a parameter
        /// </summary>
        Type ParameterType { get; }

        /// <summary>
        /// Recovers from the specified exception
        /// </summary>
        /// <param name="exception">The exception that occured</param>
        /// <param name="context">The context in which the exception has occured</param>
        /// <param name="parameter">The parameter for the recovery</param>
        RecoveryResult Recover( Exception exception, IReadOnlyDictionary<string, string> context, object parameter );
    }
}