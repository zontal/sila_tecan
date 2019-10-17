using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace Tecan.Sila2
{
    /// <summary>
    /// Utility class to serialize and deserialize features
    /// </summary>
    public class FeatureSerializer
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(Feature));

        /// <summary>
        /// Saves the given feature to the given path
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="path">A path where to store the feature</param>
        public static void Save(Feature feature, string path)
        {
            using (var writer = new StreamWriter(path))
            {
                Serializer.Serialize(writer, feature);
            }
        }

        /// <summary>
        /// Saves the given feature to a string
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <returns>The XML representation of the feature</returns>
        public static string SaveToString(Feature feature)
        {
            using (var writer = new Utf8StringWriter())
            {
                Serializer.Serialize(writer, feature);
                return writer.ToString();
            }
        }

        private sealed class Utf8StringWriter : StringWriter
        {
            private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

            public override Encoding Encoding => Utf8Encoding;
        }

        /// <summary>
        /// Loads a feature from the given XML string
        /// </summary>
        /// <param name="xml">The XML representation of the feature</param>
        /// <returns>The deserialized feature</returns>
        public static Feature LoadFromXml(string xml)
        {
            return (Feature) Serializer.Deserialize(new StringReader(xml));
        }

        /// <summary>
        /// Loads a feature from the given path
        /// </summary>
        /// <param name="path">The path where to load the feature from</param>
        /// <returns>The deserialized feature</returns>
        public static Feature Load(string path)
        {
            using (var reader = new StreamReader(path))
            {
                return (Feature) Serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Loads a feature from the embedded resource of an assembly
        /// </summary>
        /// <param name="assembly">The assembly that has the feature included</param>
        /// <param name="name">The name suffix of the manifest resource</param>
        /// <returns>The deserialized feature</returns>
        public static Feature LoadFromAssembly(Assembly assembly, string name)
        {
            var actualName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(name));
            if (actualName != null)
            {
                var stream = assembly.GetManifestResourceStream(actualName);
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return (Feature) Serializer.Deserialize(reader);
                    }
                }
            }

            throw new InvalidOperationException(
                $"The requested embedded resource {name} could not be found in {assembly.FullName}.");
        }
    }
}
