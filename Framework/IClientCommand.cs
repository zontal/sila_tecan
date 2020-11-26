namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Denotes a command that is executed on a remote server
    /// </summary>
    public interface IClientCommand : IObservableCommand
    {
        /// <summary>
        /// Gets the command identifier
        /// </summary>
        CommandExecutionUuid CommandId
        {
            get;
        }
    }
}
