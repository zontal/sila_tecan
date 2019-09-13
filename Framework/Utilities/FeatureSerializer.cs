using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace Tecan.Sila2
{
    public class FeatureSerializer
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(Feature));

        public static void Save(Feature feature, string path)
        {
            using (var writer = new StreamWriter(path))
            {
                Serializer.Serialize(writer, feature);
            }
        }

        public static string SaveToString(Feature feature)
        {
            using (var writer = new UTF8StringWriter())
            {
                Serializer.Serialize(writer, feature);
                return writer.ToString();
            }
        }

        private sealed class UTF8StringWriter : StringWriter
        {
            private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

            public override Encoding Encoding => Utf8Encoding;
        }

        public static Feature LoadFromXml(string xml)
        {
            return (Feature) Serializer.Deserialize(new StringReader(xml));
        }

        public static Feature Load(string path)
        {
            using (var reader = new StreamReader(path))
            {
                return (Feature) Serializer.Deserialize(reader);
            }
        }

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
