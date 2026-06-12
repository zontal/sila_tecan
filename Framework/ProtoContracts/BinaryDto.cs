using System;
using System.ComponentModel;
using System.IO;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer type for binaries
    /// </summary>
    [ProtoContract()]
    public class BinaryDto : ISilaTransferObject<Stream>
    {
        /// <summary>
        /// Creates an empty instance
        /// </summary>
        public BinaryDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="data">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public BinaryDto( Stream data, IBinaryStore store )
        {
            if(data == null)
            {
                Value = new byte[0];
            }
            else if(store.ShouldStoreBinary( data.Length ))
            {
                BinaryTransferUUID = store.StoreBinary( data );
            }
            else
            {
                using(var ms = new MemoryStream())
                {
                    data.CopyTo( ms );
                    Value = ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Creates a transfer object for the given binary array
        /// </summary>
        /// <param name="data">The binary array</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public BinaryDto( byte[] data, IBinaryStore store )
        {
            if(store.ShouldStoreBinary( data.LongLength ))
            {
                using(var ms = new MemoryStream( data ))
                {
                    BinaryTransferUUID = store.StoreBinary( ms );
                }
            }
            else
            {
                Value = data;
            }
        }

        /// <summary>
        /// Create a binary transfer object for the given file
        /// </summary>
        /// <param name="file">The file that should be stored</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public BinaryDto( FileInfo file, IBinaryStore store )
        {
            BinaryTransferUUID = store.StoreBinary( file );
        }

        /// <summary>
        /// The actual value for this dto
        /// </summary>
        [ProtoMember( 1, Name = @"value" )]
        public byte[] Value
        {
            get { return __pbn__union.Is( 1 ) ? ((byte[])__pbn__union.Object) : default; }
            set { __pbn__union = new DiscriminatedUnionObject( 1, value ); }
        }
        /// <summary>
        /// Gets a value indicating whether the value property should be serialized
        /// </summary>
        /// <returns>True, if the value property should be serialized, otherwise False</returns>
        public bool ShouldSerializeValue() => __pbn__union.Is( 1 );
        /// <summary>
        /// Resets the Value property
        /// </summary>
        public void ResetValue() => DiscriminatedUnionObject.Reset( ref __pbn__union, 1 );

        private DiscriminatedUnionObject __pbn__union;

        /// <summary>
        /// Gets the binary storage identifier
        /// </summary>
        [ProtoMember( 2 )]
        [DefaultValue( "" )]
        public string BinaryTransferUUID
        {
            get { return __pbn__union.Is( 2 ) ? ((string)__pbn__union.Object) : ""; }
            set { __pbn__union = new DiscriminatedUnionObject( 2, value ); }
        }
        /// <summary>
        /// Gets a value indicating whether the binary storage identifier property should be serialized
        /// </summary>
        /// <returns>True, if the binary storage identifier property should be serialized, otherwise False</returns>
        public bool ShouldSerializeBinaryTransferUUID() => __pbn__union.Is( 2 );

        /// <summary>
        /// Resets the binary storage identifier property
        /// </summary>
        public void ResetBinaryTransferUUID() => DiscriminatedUnionObject.Reset( ref __pbn__union, 2 );

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public Stream Extract( IBinaryStore resolver )
        {
            if(ShouldSerializeValue())
            {
                return new MemoryStream( Value );
            }
            else if(ShouldSerializeBinaryTransferUUID())
            {
                return resolver.ResolveStoredBinary( BinaryTransferUUID );
            }
            else
            {
                throw new InvalidOperationException( "Message contained neither bytes nor binary storage identifier" );
            }
        }

        /// <summary>
        /// Extracts the value from the dto into a byte array
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public byte[] ExtractToBytes( IBinaryStore resolver )
        {
            if(ShouldSerializeValue())
            {
                return Value;
            }
            else if(ShouldSerializeBinaryTransferUUID())
            {
                using(var ms = new MemoryStream())
                {
                    using(var resolvedStream = resolver.ResolveStoredBinary( BinaryTransferUUID ))
                    {
                        resolvedStream.CopyTo( ms );
                        return ms.ToArray();
                    }
                }
            }
            else
            {
                throw new InvalidOperationException( "Message contained neither bytes nor binary storage identifier" );
            }
        }

        /// <summary>
        /// Extracts the value from the dto into a byte array
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <param name="parameterName">The name of the parameter for which the value should be extracted</param>
        /// <returns>The actual value</returns>
        public byte[] TryExtractToBytes( IBinaryStore resolver, string parameterName )
        {
            try
            {
                return ExtractToBytes( resolver );
            }
            catch(Exception ex)
            {
                throw new ArgumentException( ex.Message, parameterName, ex );
            }
        }

        /// <summary>
        /// Extracts the value from the dto into a file info
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public FileInfo ExtractToFileInfo( IBinaryStore resolver )
        {
            if(ShouldSerializeValue())
            {
                var path = Path.GetTempFileName();
                File.WriteAllBytes( path, Value );
                return new FileInfo( path );
            }
            else if(ShouldSerializeBinaryTransferUUID())
            {
                return resolver.ResolveStoredBinaryFile( BinaryTransferUUID );
            }
            else
            {
                throw new InvalidOperationException( "Message contained neither bytes nor binary storage identifier" );
            }
        }

        /// <summary>
        /// Extracts the value from the dto into a file info
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <param name="parameterName">The name of the parameter for which the value should be extracted</param>
        /// <returns>The actual value</returns>
        public FileInfo TryExtractToFileInfo( IBinaryStore resolver, string parameterName )
        {
            try
            {
                return ExtractToFileInfo( resolver );
            }
            catch(Exception ex)
            {
                throw new ArgumentException( ex.Message, parameterName, ex );
            }
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            return null;
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static BinaryDto Create( Stream value, IBinaryStore store )
        {
            return new BinaryDto( value, store );
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static BinaryDto Create( byte[] value, IBinaryStore store )
        {
            return new BinaryDto( value, store );
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static BinaryDto Create( FileInfo value, IBinaryStore store )
        {
            return new BinaryDto( value, store );
        }
    }
}
