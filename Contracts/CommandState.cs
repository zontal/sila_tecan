namespace Tecan.Sila2
{
    /// <summary>
    /// Describes the state of a command
    /// </summary>
    public enum CommandState
    {
        /// <summary>
        /// The command is queued for execution
        /// </summary>
        NotStarted = 0,
        /// <summary>
        /// Execution of the command has started, but the command is still running
        /// </summary>
        Running = 1,
        /// <summary>
        /// Command execution has finished, but resulted in an unhandled exception
        /// </summary>
        FinishedWithErrors = 2,
        /// <summary>
        /// The command completed successfully
        /// </summary>
        FinishedSuccess = 3,
    }
}
