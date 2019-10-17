namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes that a network interface could not be found
    /// </summary>
    public class NetworkInterfaceNotFoundException : System.Exception
    {
        /// <inheritdoc />
        public NetworkInterfaceNotFoundException (string message) : base (message)
        { }
    }
}