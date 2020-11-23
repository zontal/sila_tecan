using System;
using System.Runtime.Serialization;

namespace Tecan.Sila2
{
    /// <summary>
    /// Server configuration data containing server properties which may change 
    /// at runtime or are configurable.
    /// </summary>
    [DataContract]
    public class ServerConfig
    {
        /// <summary>
        /// Configurable name of the server
        /// </summary>
        [field: DataMember]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Guid of the server, loaded from config or generated
        /// </summary>
        [field: DataMember]
        public Guid Uuid
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="uuid"></param>
        public ServerConfig(string name, Guid uuid)
        {
            Name = name;
            Uuid = uuid;
        }
    }
}