using System;

namespace Tecan.Sila2.Generator.Test.SilaService
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

        [SilaDisplayName("Server Name")]
        [SilaDescription("Human readable name of the SiLA Server.")]
        string ServerName
        {
            get;
        }

        [SilaDisplayName("Server Type")]
        [SilaDescription("The type of Server this is. Is specified by the implementer of the server and not" +
            " unique.")]
        string ServerType
        {
            get;
        }

        [SilaDisplayName("Server UUID")]
        [SilaDescription("Globally unique identifier that identifies a SiLA Server. The Server UUID MUST\n" +
            "be generated once and always remain the same.")]
        [PatternConstraint("[0-9a-f]{8}\\-[0-9a-f]{4}\\-[0-9a-f]{4}\\-[0-9a-f]{4}\\-[0-9a-f]{12}")]
        string ServerUUID
        {
            get;
        }

        [SilaDisplayName("Server Description")]
        [SilaDescription("Description of the SiLA Server.")]
        string ServerDescription
        {
            get;
        }

        [SilaDisplayName("Server Version")]
        [SilaDescription(@"Returns the version of the SiLA Server. A ""Major"" and a ""Minor"" version number (e.g. 1.0) MUST be provided,
            a Patch version number MAY be provided. Optionally, an arbitrary text, separated by an underscore MAY be appended, e.g. “3.19.373_mighty_lab_devices”
        ")]
        [PatternConstraint("(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(\\.(0|[1-9][0-9]*))?(_[_a-zA-Z0-9]+)?")]
        string ServerVersion
        {
            get;
        }

        [SilaDisplayName("Server Vendor URL")]
        [SilaDescription("Returns the URL to the website of the vendor or the website \nof the p" +
            "roduct of this SiLA Server.")]
        Url ServerVendorURL
        {
            get;
        }

        [SilaDisplayName("Implemented Features")]
        [SilaDescription("Returns a list of qualified Feature identifiers of all \nimplemented F" +
            "eatures of this SiLA Server.")]
        System.Collections.Generic.ICollection<FeatureIdentifier> ImplementedFeatures
        {
            get;
        }

        [SilaDisplayName("Get Feature Definition")]
        [SilaDescription("Get all details on one Feature through the qualified Feature id.")]
        [Throws(typeof(UnimplementedFeatureException))]
        [return: SilaIdentifier("FeatureDefinition")]
        [return: SilaDisplayName("Feature Definition")]
        [return: SilaDescription("The Feature Definition in XML format.")]
        FeatureDefinition GetFeatureDefinition([SilaDisplayName("Qualified Feature Identifier")] [SilaDescription("The qualified Feature identifier for which the Feature description should be retr" +
            "ieved.")] FeatureIdentifier qualifiedFeatureIdentifier);

        [SilaDisplayName("Set Server Name")]
        [SilaDescription("Sets a human readable name to the Server Name property")]
        void SetServerName([SilaDisplayName("Server Name")] [SilaDescription("The human readable name of to assign to the SiLA Server")] string serverName);

    }

    [SilaDisplayName("Feature Identifier")]
    [SilaDescription("Qualified Feature Identifier as provided by the ImplementedFeatures property.")]
    [MaximalLength(255)]
    [SilaIdentifierType(IdentifierType.FeatureIdentifier)]
    public struct FeatureIdentifier
    {

        public FeatureIdentifier(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    [SilaDisplayName("Feature Definition")]
    [SilaDescription("The content of a Feature Definition conforming with the XML Schema.")]
    [Schema("https://gitlab.com/SiLA2/sila_base/raw/master/schema/FeatureDefinition.xsd", SchemaType.Xml)]
    public struct FeatureDefinition
    {
        public FeatureDefinition(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    [SilaIdentifier("URL")]
    [SilaDescription("Uniform Resource Locator as defined in RFC 1738.")]
    [PatternConstraint("https?://.+")]
    public struct Url
    {
        public Url(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    [SilaDisplayName("Unimplemented Feature")]
    [SilaDescription("The feature specified by the given feature identifier is not implemented by the s" +
        "erver.")]
    public class UnimplementedFeatureException : Exception
    {
        public UnimplementedFeatureException(string message) : base(message) { }
    }
}
