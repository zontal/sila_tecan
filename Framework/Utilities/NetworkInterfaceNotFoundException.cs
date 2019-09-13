namespace Tecan.Sila2
{
    public class NetworkInterfaceNotFoundException : System.Exception
    {
        public NetworkInterfaceNotFoundException (string message) : base (message)
        { }
    }
}