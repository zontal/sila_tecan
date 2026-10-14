using System;
using System.IO;
using System.Xml;

namespace Tecan.Sila2
{
    /// <summary>
    /// Utility class to serialize and deserialize data types
    /// </summary>
    public static class DataTypeSerializer
    {
        private static readonly XmlWriterSettings settings = new XmlWriterSettings()
        {
            Indent = false,
            OmitXmlDeclaration = true,
        };

        /// <summary>
        /// Saves the given data type to a string
        /// </summary>
        /// <param name="dataType">The data type</param>
        /// <returns>The XML representation of the data type</returns>
        public static string SaveToString( DataTypeType dataType )
        {
            using( var writer = new Utf8StringWriter() )
            {
                using (var xmlWriter = XmlWriter.Create(writer, settings))
                {
                    FeatureDefinitionXml.WriteDataType( xmlWriter, dataType );
                    return writer.ToString();
                }
            }
        }

        /// <summary>
        /// Loads a data type from the given XML string
        /// </summary>
        /// <param name="xml">The XML representation of the data type</param>
        /// <returns>The deserialized data type</returns>
        public static DataTypeType LoadType( string xml )
        {
            return FeatureDefinitionXml.ReadDataType( new StringReader( xml ) );
        }
    }
}

