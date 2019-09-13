namespace Tecan.Sila2
{
    /// <summary>
    /// Class representing the static information of a SiLA Server
    /// </summary>
    public class ServerInformation
    {
        /// <summary>
        /// The Type of the server usually the Make and Model
        /// </summary>
        /// <value></value>
        public string Type { get; }

        /// <summary>
        /// Description of the server, i.e. what it does and its features it exposes
        /// </summary>
        /// <value></value>
        public string Description { get; }

        /// <summary>
        /// Typically the URL to the vendor website
        /// </summary>
        /// <value></value>
        public string VendorURI { get; }

        /// <summary>
        /// Version of the server 
        /// </summary>
        /// <value></value>
        public string Version { get; }

        /// <summary>
        /// Constructor of the Server information
        /// </summary>
        /// <param name="type"></param>
        /// <param name="description"></param>
        /// <param name="vendorURI"></param>
        /// <param name="version"></param>
        public ServerInformation(string type, string description, string vendorURI, string version)
        {
            Type = type;
            Description = description;
            VendorURI = vendorURI;
            Version = version;
        }

        /// <summary>
        /// Prints all server information
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Server Type: {Type}"
            + $"\nServer Description: {Description}"
            + $"\nServer VendorURI: {VendorURI}"
            + $"\nServer Version: {Version}\n";
        }
    }
}