namespace Tecan.Sila2
{
    /// <summary>
    /// Interface for a data transfer object used to request a command
    /// </summary>
    public interface ISilaRequestObject : ISilaTransferObject
    {
        /// <summary>
        /// The fully qualified identifier of the command
        /// </summary>
        string CommandIdentifier { get; }
    }
}