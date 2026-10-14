using System.Diagnostics.CodeAnalysis;
using Grpc.Core;
using ProtoBuf.Meta;

namespace Tecan.Sila2
{
    /// <summary>
    /// A generic marshaller implementation based on Protobuf-net
    /// </summary>
    /// <typeparam name="T">The data transfer object type</typeparam>
    /// <remarks>
    /// The members of this class use the reflection-based protobuf-net runtime model. Use <see cref="ProtobufMarshaller"/>
    /// to create marshallers that are compatible with trimming and Native AOT.
    /// </remarks>
    [RequiresUnreferencedCode( TrimmingMessages.RuntimeTypeModel )]
    [RequiresDynamicCode( TrimmingMessages.RuntimeTypeModel )]
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
            return ProtobufMarshaller.FromByteSerializer( serializer );
        }
    }

    /// <summary>
    /// Creates marshallers for data transfer objects that are compatible with trimming and Native AOT
    /// </summary>
    public static class ProtobufMarshaller
    {
        /// <summary>
        /// Converts the given byte serializer to a Marshaller
        /// </summary>
        /// <typeparam name="T">The data transfer object type</typeparam>
        /// <param name="serializer">The serializer</param>
        /// <returns>A Marshaller</returns>
        public static Marshaller<T> FromByteSerializer<T>( ByteSerializer<T> serializer )
        {
            return new Marshaller<T>( serializer.Serializer, serializer.Deserializer );
        }

        /// <summary>
        /// Creates a Marshaller that uses the given protobuf-net type model
        /// </summary>
        /// <typeparam name="T">The data transfer object type</typeparam>
        /// <param name="model">The protobuf-net type model that is able to serialize <typeparamref name="T"/>, usually a compile-time generated model such as <see cref="FrameworkTypeModel"/></param>
        /// <returns>A Marshaller</returns>
        public static Marshaller<T> Create<T>( TypeModel model )
        {
            return FromByteSerializer( ByteSerializer.Create<T>( model ) );
        }
    }
}
