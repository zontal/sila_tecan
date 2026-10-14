using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Diagnostics.CodeAnalysis;
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
        [RequiresUnreferencedCode( TrimmingMessages.RuntimeTypeModel )]
        [RequiresDynamicCode( TrimmingMessages.RuntimeTypeModel )]
        public static T FromByteArray<T>( byte[] data )
        {
            using(var ms = new MemoryStream( data ))
            {
#pragma warning disable PBN3010 // this member deliberately uses the runtime model, the model-based overload is the AOT-compatible alternative
                return Serializer.Deserialize<T>( ms );
#pragma warning restore PBN3010
            }
        }

        /// <summary>
        /// Converts the given data transfer object to a byte array
        /// </summary>
        /// <param name="instance">The data transfer object</param>
        /// <returns>A byte array</returns>
        [RequiresUnreferencedCode( TrimmingMessages.RuntimeTypeModel )]
        [RequiresDynamicCode( TrimmingMessages.RuntimeTypeModel )]
        public static byte[] ToByteArray<T>( T instance )
        {
            using(var ms = new MemoryStream())
            {
#pragma warning disable PBN3010 // this member deliberately uses the runtime model, the model-based overload is the AOT-compatible alternative
                Serializer.Serialize( ms, instance );
#pragma warning restore PBN3010
                var data = ms.ToArray();
                return data;
            }
        }

        /// <summary>
        /// Create the default byte serializer for the type T
        /// </summary>
        /// <typeparam name="T">The type that should be transported</typeparam>
        /// <returns>A byte serializer instance</returns>
        [RequiresUnreferencedCode( TrimmingMessages.RuntimeTypeModel )]
        [RequiresDynamicCode( TrimmingMessages.RuntimeTypeModel )]
        public static ByteSerializer<T> GetDefault<T>()
        {
            return new ByteSerializer<T>( ToByteArray, FromByteArray<T> );
        }

        /// <summary>
        /// Extracts a data transfer object from the given data using the given protobuf-net type model
        /// </summary>
        /// <typeparam name="T">The data transfer object type</typeparam>
        /// <param name="data">The data</param>
        /// <param name="model">The protobuf-net type model that is able to serialize <typeparamref name="T"/>, usually a compile-time generated model</param>
        /// <returns>A data transfer object instance</returns>
        public static T FromByteArray<[DynamicallyAccessedMembers( TrimmingAnnotations.DataTransferObjectMembers )] T>( byte[] data, TypeModel model )
        {
            if(model == null)
            {
                throw new ArgumentNullException( nameof( model ) );
            }
            using(var ms = new MemoryStream( data ))
            {
                return model.Deserialize<T>( ms );
            }
        }

        /// <summary>
        /// Converts the given data transfer object to a byte array using the given protobuf-net type model
        /// </summary>
        /// <typeparam name="T">The data transfer object type</typeparam>
        /// <param name="instance">The data transfer object</param>
        /// <param name="model">The protobuf-net type model that is able to serialize <typeparamref name="T"/>, usually a compile-time generated model</param>
        /// <returns>A byte array</returns>
        public static byte[] ToByteArray<[DynamicallyAccessedMembers( TrimmingAnnotations.DataTransferObjectMembers )] T>( T instance, TypeModel model )
        {
            if(model == null)
            {
                throw new ArgumentNullException( nameof( model ) );
            }
            using(var ms = new MemoryStream())
            {
                model.Serialize( ms, instance );
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Creates a byte serializer for the type T that uses the given protobuf-net type model
        /// </summary>
        /// <typeparam name="T">The type that should be transported</typeparam>
        /// <param name="model">The protobuf-net type model that is able to serialize <typeparamref name="T"/>, usually a compile-time generated model such as <see cref="FrameworkTypeModel"/></param>
        /// <returns>A byte serializer instance</returns>
        /// <remarks>Byte serializers created from a compile-time generated model are compatible with trimming and Native AOT.</remarks>
        public static ByteSerializer<T> Create<[DynamicallyAccessedMembers( TrimmingAnnotations.DataTransferObjectMembers )] T>( TypeModel model )
        {
            if(model == null)
            {
                throw new ArgumentNullException( nameof( model ) );
            }
            return new ByteSerializer<T>( instance => ToByteArray( instance, model ), data => FromByteArray<T>( data, model ) );
        }
    }
}
