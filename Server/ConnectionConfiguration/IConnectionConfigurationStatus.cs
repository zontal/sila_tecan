using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server.ConnectionConfiguration
{
    /// <summary>
    /// Interface to query the status of connected pool servers
    /// </summary>
    public interface IConnectionConfigurationStatus
    {
        /// <summary>
        /// Starts all configured server-initiated connections
        /// </summary>
        void Start();
    }

    public partial class ConnectionConfigurationServiceProvider
    {
        internal IConnectionConfigurationStatus ConnectionConfigurationStatus => _implementation as IConnectionConfigurationStatus;
    }
}
