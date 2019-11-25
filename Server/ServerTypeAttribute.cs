using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes the SiLA2 Server type of this server
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ServerTypeAttribute : Attribute
    {
        /// <summary>
        /// Gets the server type
        /// </summary>
        public string ServerType
        {
            get;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="serverType">The SiLA2 Server Type</param>
        public ServerTypeAttribute( string serverType )
        {
            ServerType = serverType;
        }
    }
}
