using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Tecan.Sila2
{
    /// <summary>
    /// Helper class for serializing constraints
    /// </summary>
    public static class ConstraintSerializer
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer( typeof( Constraints ) );



        /// <summary>
        /// Saves the given constraints to a string
        /// </summary>
        /// <param name="constraints">The constraints</param>
        /// <returns>The XML representation of the constraints</returns>
        public static string SaveToString( Constraints constraints )
        {
            using(var writer = new Utf8StringWriter())
            {
                Serializer.Serialize( writer, constraints );
                return writer.ToString();
            }
        }

        /// <summary>
        /// Loads constraints from the given xml string
        /// </summary>
        /// <param name="xml">The XML representation of the constraints or constrained type</param>
        /// <returns>The deserialized constraints</returns>
        public static Constraints LoadConstraints( string xml )
        {
            return Serializer.Deserialize( new StringReader( xml ) ) as Constraints;
        }
    }
}
