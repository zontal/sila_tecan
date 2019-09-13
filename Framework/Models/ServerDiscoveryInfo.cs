using System.Net;

namespace Tecan.Sila2
{
    /// <summary>
    /// IPAddress and Port representing a service
    /// </summary>
    public class ServerDiscoveryInfo
    {
        /// <summary>
        /// Address of service
        /// </summary>
        public IPAddress Host { get; }
        /// <summary>
        /// Port where service is hosted from
        /// </summary>
        public int Port { get; }
        /// <summary>
        /// Guid representing discovery ID
        /// </summary>
        /// 
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public ServerDiscoveryInfo(IPAddress host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}