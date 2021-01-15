namespace Tecan.Sila2
{
    /// <summary>
    /// Describes the state of a command
    /// </summary>
    public enum CommandState
    {
        /// <summary>
        /// The state of the command is unknown
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The command is queued for execution
        /// </summary>
        NotStarted,
        /// <summary>
        /// Execution of the command has started, but the command is still running
        /// </summary>
        Running,
        /// <summary>
        /// Command execution has finished, but resulted in an unhandled exception
        /// </summary>
        FinishedWithErrors,
        /// <summary>
        /// The command completed successfully
        /// </summary>
        FinishedSuccess,
    }
}
