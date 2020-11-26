using ProtoBuf;
using System.IO;

namespace Tecan.Sila2
{
    /// <summary>
    /// Helper class to serialize data transfer objects to byte strings
    /// </summary>
    public static class ByteSerializer
    {
        /// <summary>
        /// Extracts a data transfer object from the given data
        /// </summary>
        /// <param name="data">The data</param>
        /// <returns>A data transfer object instance</returns>
        public static T FromByteArray<T>( byte[] data )
        {
            using(var ms = new MemoryStream( data ))
            {
                return Serializer.Deserialize<T>( ms );
            }
        }

        /// <summary>
        /// Converts the given data transfer object to a byte array
        /// </summary>
        /// <param name="instance">The data transfer object</param>
        /// <returns>A byte array</returns>
        public static byte[] ToByteArray<T>( T instance )
        {
            using(var ms = new MemoryStream())
            {
                Serializer.Serialize( ms, instance );
                var data = ms.ToArray();
                return data;
            }
        }
    }
}
