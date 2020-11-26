using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes a pair of serialization and deserialization methods
    /// </summary>
    /// <typeparam name="T">The data transfer object type</typeparam>
    public readonly struct ByteSerializer<T>
    {
        /// <summary>
        /// A method to serialize an instance of <typeparamref name="T"/>
        /// </summary>
        public Func<T, byte[]> Serializer
        {
            get;
        }

        /// <summary>
        /// A method to deserialize an instance of <typeparamref name="T"/>
        /// </summary>
        public Func<byte[], T> Deserializer
        {
            get;
        }

        /// <summary>
        /// Creates a new pair of serialization functions
        /// </summary>
        /// <param name="serializer">A method to serialize an instance of <typeparamref name="T"/></param>
        /// <param name="deserializer">A method to deserialize an instance of <typeparamref name="T"/></param>
        public ByteSerializer(Func<T, byte[]> serializer, Func<byte[], T> deserializer)
        {
            Serializer = serializer;
            Deserializer = deserializer;
        }
    }
}
