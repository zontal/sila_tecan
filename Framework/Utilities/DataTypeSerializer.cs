using System.IO;
using System.Xml.Serialization;

namespace Tecan.Sila2
{
    /// <summary>
    /// Utility class to serialize and deserialize data types
    /// </summary>
    public class DataTypeSerializer
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer( typeof(DataTypeType) );

        /// <summary>
        /// Saves the given data type to a string
        /// </summary>
        /// <param name="dataType">The data type</param>
        /// <returns>The XML representation of the data type</returns>
        public static string SaveToString( DataTypeType dataType )
        {
            using( var writer = new Utf8StringWriter() )
            {
                Serializer.Serialize( writer, dataType );
                return writer.ToString();
            }
        }

        /// <summary>
        /// Loads a data type from the given XML string
        /// </summary>
        /// <param name="xml">The XML representation of the data type</param>
        /// <returns>The deserialized data type</returns>
        public static DataTypeType LoadType( string xml )
        {
            return (DataTypeType)Serializer.Deserialize( new StringReader( xml ) );
        }
    }
}

