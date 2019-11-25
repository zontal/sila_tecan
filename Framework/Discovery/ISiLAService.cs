using System;
using System.Collections.Generic;

namespace Tecan.Sila2.Discovery
{
    /// <summary>
    /// The Feature each SiLA Server MUST implement. It is the entry point to a SiLA Server and helps to discover the Features it implements.
    /// </summary>
    [SilaFeature(true, null)]
    [SilaIdentifier("SiLAService")]
    [SilaDisplayName("SiLA Service")]
    [SilaDescription("The Feature each SiLA Server MUST implement. It is the entry point to a SiLA Server and helps to discover the Features it implements.")]
    public interface ISiLAService
    {
        /// <summary>
        /// Human readable name of the SiLA Server.
        /// </summary>
        [SilaDisplayName("Server Name")]
        [SilaDescription("Human readable name of the SiLA Server.")]
        string ServerName
        {
            get;
        }

        /// <summary>
        /// The type of Server this is. Is specified by the implementer of the server and not unique.
        /// </summary>
        [SilaDisplayName("Server Type")]
        [SilaDescription("The type of Server this is. Is specified by the implementer of the server and not" +
            " unique.")]
        string ServerType
        {
            get;
        }

        /// <summary>
        /// Globally unique identifier that identifies a SiLA Server. The Server UUID MUST be generated once and always remain the same.
        /// </summary>
        [SilaDisplayName("Server UUID")]
        [SilaDescription("Globally unique identifier that identifies a SiLA Server. The Server UUID MUST\n" +
            "be generated once and always remain the same.")]
        [PatternConstraint("[0-9a-f]{8}\\-[0-9a-f]{4}\\-[0-9a-f]{4}\\-[0-9a-f]{4}\\-[0-9a-f]{12}")]
        string ServerUUID
        {
            get;
        }

        /// <summary>
        /// Description of the SiLA Server.
        /// </summary>
        [SilaDisplayName("Server Description")]
        [SilaDescription("Description of the SiLA Server.")]
        string ServerDescription
        {
            get;
        }

        /// <summary>
        /// Returns the version of the SiLA Server. A "Major" and a "Minor" version number (e.g. 1.0) MUST be provided, a Patch version number MAY be provided. Optionally, an arbitrary text, separated by an underscore MAY be appended, e.g. “3.19.373_mighty_lab_devices
        /// </summary>
        [SilaDisplayName("Server Version")]
        [SilaDescription(@"Returns the version of the SiLA Server. A ""Major"" and a ""Minor"" version number (e.g. 1.0) MUST be provided,
            a Patch version number MAY be provided. Optionally, an arbitrary text, separated by an underscore MAY be appended, e.g. “3.19.373_mighty_lab_devices”
        ")]
        [PatternConstraint("(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(\\.(0|[1-9][0-9]*))?(_[_a-zA-Z0-9]+)?")]
        string ServerVersion
        {
            get;
        }

        /// <summary>
        /// Returns the URL to the website of the vendor or the website of the product of this SiLA Server.
        /// </summary>
        [SilaDisplayName("Server Vendor URL")]
        [SilaDescription("Returns the URL to the website of the vendor or the website \nof the p" +
            "roduct of this SiLA Server.")]
        string ServerVendorURL
        {
            get;
        }

        /// <summary>
        /// Returns a list of qualified Feature identifiers of all implemented Features of this SiLA Server.
        /// </summary>
        [SilaDisplayName("Implemented Features")]
        [SilaDescription("Returns a list of qualified Feature identifiers of all \nimplemented F" +
            "eatures of this SiLA Server.")]
        ICollection<string> ImplementedFeatures
        {
            get;
        }

        /// <summary>
        /// Get all details on one Feature through the qualified Feature id.
        /// </summary>
        /// <param name="qualifiedFeatureIdentifier">The qualified Feature identifier for which the Feature description should be retrieved.</param>
        /// <returns>The Feature Definition in XML format.</returns>
        [SilaDisplayName("Get Feature Definition")]
        [SilaDescription("Get all details on one Feature through the qualified Feature id.")]
        [Throws(typeof(UnimplementedFeatureException))]
        [return: SilaIdentifier("FeatureDefinition")]
        [return: SilaDisplayName("Feature Definition")]
        [return: SilaDescription("The Feature Definition in XML format.")]
        string GetFeatureDefinition([SilaDisplayName("Qualified Feature Identifier")] [SilaDescription("The qualified Feature identifier for which the Feature description should be retr" +
            "ieved.")] string qualifiedFeatureIdentifier);

        /// <summary>
        /// Sets a human readable name to the Server Name property
        /// </summary>
        /// <param name="serverName">The human readable name of to assign to the SiLA Server</param>
        [SilaDisplayName("Set Server Name")]
        [SilaDescription("Sets a human readable name to the Server Name property")]
        void SetServerName([SilaDisplayName("Server Name")] [SilaDescription("The human readable name of to assign to the SiLA Server")] string serverName);

    }

    /// <summary>
    /// The feature specified by the given feature identifier is not implemented by the server.
    /// </summary>
    [SilaDisplayName("Unimplemented Feature")]
    [SilaDescription("The feature specified by the given feature identifier is not implemented by the s" +
        "erver.")]
    public class UnimplementedFeatureException : Exception
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="message">The error message</param>
        public UnimplementedFeatureException(string message) : base(message) { }
    }
}
