using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Denotes a lazy binary stream
    /// </summary>
    public class LazyBinaryStream : Stream
    {
        private readonly Lazy<Stream> _streamImplementation;

        /// <summary>
        /// Gets the binary storage identifier associated with this lazy binary stream
        /// </summary>
        public string BinaryStorageIdentifier
        {
            get;
        }

        /// <inheritdoc />
        public LazyBinaryStream( IBinaryStore binaryResolver, string binaryIdentifier )
        {
            BinaryStorageIdentifier = binaryIdentifier;
            _streamImplementation = new Lazy<Stream>(() => binaryResolver.ResolveStoredBinary(binaryIdentifier), true);
        }

        /// <inheritdoc />
        public override void Flush()
        {
            _streamImplementation.Value.Flush();
        }

        /// <inheritdoc />
        public override int Read( byte[] buffer, int offset, int count )
        {
            return _streamImplementation.Value.Read( buffer, offset, count );
        }

        /// <inheritdoc />
        public override long Seek( long offset, SeekOrigin origin )
        {
            return _streamImplementation.Value.Seek( offset, origin );
        }

        /// <inheritdoc />
        public override void SetLength( long value )
        {
            _streamImplementation.Value.SetLength( value );
        }

        /// <inheritdoc />
        public override void Write( byte[] buffer, int offset, int count )
        {
            _streamImplementation.Value.Write( buffer, offset, count );
        }

        /// <inheritdoc />
        public override bool CanRead => _streamImplementation.Value.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => _streamImplementation.Value.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => _streamImplementation.Value.CanWrite;

        /// <inheritdoc />
        public override long Length => _streamImplementation.Value.Length;

        /// <inheritdoc />
        public override long Position
        {
            get => _streamImplementation.Value.Position;
            set => _streamImplementation.Value.Position = value;
        }
    }
}
