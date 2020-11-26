using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tecan.Sila2.Binary
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

        private readonly IBinaryStore _binaryStore;

        private Stream CreateStream()
        {
            return _binaryStore.ResolveStoredBinary( BinaryStorageIdentifier );
        }

        /// <inheritdoc />
        public LazyBinaryStream( IBinaryStore binaryResolver, string binaryIdentifier )
        {
            BinaryStorageIdentifier = binaryIdentifier;
            _binaryStore = binaryResolver;
            _streamImplementation = new Lazy<Stream>(CreateStream, true);
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

        /// <inheritdoc />
        public override bool CanTimeout => _streamImplementation.Value.CanTimeout;

        /// <inheritdoc />
        public override void Close() => _streamImplementation.Value.Close();

        /// <inheritdoc />
        protected override void Dispose( bool disposing )
        {
            if( _streamImplementation.IsValueCreated )
            {
                _streamImplementation.Value.Dispose();
                _binaryStore.Delete(BinaryStorageIdentifier);
            }
            base.Dispose( disposing );
        }

        /// <inheritdoc />
        public override Task CopyToAsync( Stream destination, int bufferSize, CancellationToken cancellationToken )
        {
            return _streamImplementation.Value.CopyToAsync( destination, bufferSize, cancellationToken );
        }

        /// <inheritdoc />
        public override IAsyncResult BeginRead( byte[] buffer, int offset, int count, AsyncCallback callback, object state )
        {
            return _streamImplementation.Value.BeginRead( buffer, offset, count, callback, state );
        }

        /// <inheritdoc />
        public override IAsyncResult BeginWrite( byte[] buffer, int offset, int count, AsyncCallback callback, object state )
        {
            return _streamImplementation.Value.BeginWrite( buffer, offset, count, callback, state );
        }

        /// <inheritdoc />
        public override int EndRead( IAsyncResult asyncResult )
        {
            return _streamImplementation.Value.EndRead( asyncResult );
        }

        /// <inheritdoc />
        public override Task FlushAsync( CancellationToken cancellationToken )
        {
            return _streamImplementation.Value.FlushAsync( cancellationToken );
        }

        /// <inheritdoc />
        public override void EndWrite( IAsyncResult asyncResult )
        {
            _streamImplementation.Value.EndWrite( asyncResult );
        }

        /// <inheritdoc />
        public override object InitializeLifetimeService()
        {
            return _streamImplementation.Value.InitializeLifetimeService();
        }

        /// <inheritdoc />
        public override Task<int> ReadAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            return _streamImplementation.Value.ReadAsync( buffer, offset, count, cancellationToken );
        }

        /// <inheritdoc />
        public override int ReadByte()
        {
            return _streamImplementation.Value.ReadByte();
        }

        /// <inheritdoc />
        public override int ReadTimeout
        {
            get => _streamImplementation.Value.ReadTimeout;
            set => _streamImplementation.Value.ReadTimeout = value;
        }

        /// <inheritdoc />
        public override Task WriteAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            return _streamImplementation.Value.WriteAsync( buffer, offset, count, cancellationToken );
        }

        /// <inheritdoc />
        public override void WriteByte( byte value )
        {
            _streamImplementation.Value.WriteByte( value );
        }

        /// <inheritdoc />
        public override int WriteTimeout
        {
            get => _streamImplementation.Value.WriteTimeout;
            set => _streamImplementation.Value.WriteTimeout = value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Lazy stream for {BinaryStorageIdentifier}";
        }
    }
}
