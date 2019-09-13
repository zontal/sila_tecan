using System;
using System.Diagnostics;
using System.IO;
using Grpc.Core;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// A generic marshaller implementation based on Protobuf-net
    /// </summary>
    /// <typeparam name="T">The data transfer object type</typeparam>
    public class ProtobufMarshaller<T>
    {
        /// <summary>
        /// The default marshaller for the data transfer object type
        /// </summary>
        public static readonly Marshaller<T> Default = new Marshaller<T>(ToByteArray, FromByteArray);

        /// <summary>
        /// Extracts a data transfer object from the given data
        /// </summary>
        /// <param name="data">The data</param>
        /// <returns>A data transfer object instance</returns>
        public static T FromByteArray(byte[] data)
        {
            Debug.WriteLine(Convert.ToBase64String(data));
            using (var ms = new MemoryStream(data))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }

        /// <summary>
        /// Converts the given data transfer object to a byte array
        /// </summary>
        /// <param name="instance">The data transfer object</param>
        /// <returns>A byte array</returns>
        public static byte[] ToByteArray(T instance)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, instance);
                var data = ms.ToArray();
                Debug.WriteLine(Convert.ToBase64String(data));
                return data;
            }
        }
    }
}
