using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace Tecan.Sila2
{
    /// <summary>
    /// Reflection-free XML codec for the SiLA 2 feature definition model (Feature, DataTypeType, Constraints and their parts).
    /// It reproduces the mapping the XmlSerializer derives from the attributes of the classes generated from FeatureDefinition.xsd,
    /// so that documents are read into the same object graphs and written to the same text as with the XmlSerializer,
    /// but it does not require runtime code generation or reflection and is therefore safe for trimming and Native AOT.
    /// </summary>
    internal static partial class FeatureDefinitionXml
    {
        /// <summary>
        /// The namespace of the SiLA 2 feature definition language
        /// </summary>
        internal const string SilaNamespace = "http://www.sila-standard.org";

        private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        private const string Xsi2000Namespace = "http://www.w3.org/2000/10/XMLSchema-instance";
        private const string Xsi1999Namespace = "http://www.w3.org/1999/XMLSchema-instance";
        private const string XsdNamespace = "http://www.w3.org/2001/XMLSchema";

        internal const string FeatureElement = "Feature";
        internal const string DataTypeElement = "DataType";
        internal const string ConstraintsElement = "Constraints";

        private static readonly bool IsNetFramework = RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal );

        /// <summary>
        /// Reads a feature definition from the given text reader
        /// </summary>
        /// <param name="textReader">The text reader that contains the feature definition</param>
        /// <returns>The deserialized feature</returns>
        /// <exception cref="InvalidOperationException">Thrown if the document cannot be read</exception>
        internal static Feature ReadFeature( TextReader textReader )
        {
            return Deserialize( textReader, FeatureElement, SilaNamespace, null, false, ReadFeature );
        }

        /// <summary>
        /// Reads a data type document with the root element DataType from the given text reader
        /// </summary>
        /// <param name="textReader">The text reader that contains the data type</param>
        /// <returns>The deserialized data type</returns>
        /// <exception cref="InvalidOperationException">Thrown if the document cannot be read</exception>
        internal static DataTypeType ReadDataType( TextReader textReader )
        {
            return Deserialize( textReader, DataTypeElement, SilaNamespace, nameof( DataTypeType ), true, ReadDataTypeType );
        }

        /// <summary>
        /// Reads a constraints document with the root element Constraints from the given text reader.
        /// As with the XmlSerializer for the Constraints type, the root element has no namespace, but its children are in the SiLA namespace.
        /// </summary>
        /// <param name="textReader">The text reader that contains the constraints</param>
        /// <returns>The deserialized constraints</returns>
        /// <exception cref="InvalidOperationException">Thrown if the document cannot be read</exception>
        internal static Constraints ReadConstraints( TextReader textReader )
        {
            return Deserialize( textReader, ConstraintsElement, string.Empty, nameof( Constraints ), true, ReadConstraints );
        }

        /// <summary>
        /// Writes the given feature as an indented XML document
        /// </summary>
        /// <param name="textWriter">The text writer to write the document to</param>
        /// <param name="feature">The feature</param>
        /// <exception cref="InvalidOperationException">Thrown if the feature cannot be written</exception>
        internal static void WriteFeature( TextWriter textWriter, Feature feature )
        {
            Serialize( CreateDocumentWriter( textWriter ), FeatureElement, SilaNamespace, false, feature, WriteFeature );
        }

        /// <summary>
        /// Writes the given data type as a document with the root element DataType
        /// </summary>
        /// <param name="xmlWriter">The XML writer to write the document to</param>
        /// <param name="dataType">The data type</param>
        /// <exception cref="InvalidOperationException">Thrown if the data type cannot be written</exception>
        internal static void WriteDataType( XmlWriter xmlWriter, DataTypeType dataType )
        {
            Serialize( xmlWriter, DataTypeElement, SilaNamespace, true, dataType, WriteDataTypeType );
        }

        /// <summary>
        /// Writes the given constraints as an indented XML document with the root element Constraints
        /// </summary>
        /// <param name="textWriter">The text writer to write the document to</param>
        /// <param name="constraints">The constraints</param>
        /// <exception cref="InvalidOperationException">Thrown if the constraints cannot be written</exception>
        internal static void WriteConstraints( TextWriter textWriter, Constraints constraints )
        {
            Serialize( CreateDocumentWriter( textWriter ), ConstraintsElement, string.Empty, true, constraints, WriteConstraints );
        }

        /// <summary>
        /// Creates the XML writer the XmlSerializer uses when it serializes to a text writer
        /// </summary>
        private static XmlWriter CreateDocumentWriter( TextWriter textWriter )
        {
            if(IsNetFramework)
            {
                return new XmlTextWriter( textWriter )
                {
                    Formatting = Formatting.Indented,
                    Indentation = 2
                };
            }
            return XmlWriter.Create( textWriter, new XmlWriterSettings
            {
                Encoding = new UTF8Encoding( false ),
                Indent = true
            } );
        }
    }
}
