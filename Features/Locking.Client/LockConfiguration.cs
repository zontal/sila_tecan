using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Tecan.Sila2.Locking
{
    /// <summary>
    /// Denotes the configuration for the locks
    /// </summary>
    public class LockConfiguration
    {
        /// <summary>
        /// Gets or sets the threshold (in seconds) when locks for servers that sson expre are refreshed
        /// </summary>
        [XmlAttribute]
        public double ExpiryThreshold { get; set; }

        /// <summary>
        /// Gets or sets the time (in seconds) a server is locked initially
        /// </summary>
        [XmlAttribute]
        public double LockTime { get; set; }

        /// <summary>
        /// Gets or sets the time (in seconds) a server is locked when a previous lock has expired
        /// </summary>
        [XmlAttribute]
        public double RefreshTime { get; set; }

        /// <summary>
        /// Gets or sets the interval (in seconds) between checks for expired locks
        /// </summary>
        [XmlAttribute]
        public double ExpiryCheckInterval { get; set; }
    }
}
