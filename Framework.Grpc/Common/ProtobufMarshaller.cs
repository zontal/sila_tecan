using Grpc.Core;

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
            return ByteSerializer.FromByteArray<T>( data );
        }

        /// <summary>
        /// Converts the given data transfer object to a byte array
        /// </summary>
        /// <param name="instance">The data transfer object</param>
        /// <returns>A byte array</returns>
        public static byte[] ToByteArray(T instance)
        {
            return ByteSerializer.ToByteArray( instance );
        }

        /// <summary>
        /// Converts the given byte serializer to a Marshaller
        /// </summary>
        /// <param name="serializer">The serializer</param>
        /// <returns>A Marshaller</returns>
        public static Marshaller<T> FromByteSerializer(ByteSerializer<T> serializer)
        {
            return new Marshaller<T>( serializer.Serializer, serializer.Deserializer );
        }
    }
}
