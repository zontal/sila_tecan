using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.ServerPooling
{

    /// <summary>
    /// A default implementation for a client binary data management
    /// </summary>
    internal class PoolBinaryStore :IBinaryStore
    {
        private const int ChunkSize = 1024 * 1024 * 2;
        private readonly string _commandParameterIdentifier;
        private readonly IClientExecutionManager _executionManager;
        private readonly IBinaryStoreChannel _channel;

        /// <summary>
        /// Creates a new client binary store for the given command parameter identifier
        /// </summary>
        /// <param name="commandParameterIdentifier">A globally unique identifier for the command parameter or null, if the store is not associated to a command parameter</param>
        /// <param name="channel">The underlying channel</param>
        /// <param name="executionManager">The execution manager to handle requests to the given server</param>
        public PoolBinaryStore( string commandParameterIdentifier, IBinaryStoreChannel channel, IClientExecutionManager executionManager )
        {
            _commandParameterIdentifier = commandParameterIdentifier;
            _executionManager = executionManager;
            _channel = channel;
        }

        /// <inheritdoc />
        public Stream ResolveStoredBinary( string identifier )
        {
            var ms = new MemoryStream();
            Download( identifier, ms ).Wait();
            ms.Position = 0;
            return ms;
        }

        /// <inheritdoc />
        public bool ShouldStoreBinary( long length )
        {
            return length > 200 * 1024;
        }

        /// <inheritdoc />
        public string StoreBinary( Stream data )
        {
            var task = Upload( data );
            task.Wait();
            return task.Result;
        }

        /// <inheritdoc />
        public string StoreBinary( FileInfo file )
        {
            using(var stream = file.OpenRead())
            {
                return StoreBinary( stream );
            }
        }

        /// <summary>
        /// Uploads the given file
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <returns>An awaitable task that returns the storage identifier</returns>
        public async Task<string> Upload( Stream file )
        {
            var chunkCount = (uint)((file.Length + ChunkSize - 1) / ChunkSize);
            var identifier = await _channel.CreateBinaryAsync( new CreateBinaryRequestDto()
            {
                BinarySize = (ulong)file.Length,
                ChunkCount = chunkCount,
                CommandIdentifier = _commandParameterIdentifier
            }, CreateCallOptions() );
            var bytes = new byte[ChunkSize];
            for(uint i = 0; i < chunkCount; i++)
            {
                var bytesRead = await file.ReadAsync( bytes, 0, ChunkSize );
                byte[] bytesToSend;
                if(bytesRead == ChunkSize)
                {
                    bytesToSend = bytes;
                }
                else
                {
                    bytesToSend = new byte[bytesRead];
                    Array.Copy( bytes, bytesToSend, bytesRead );
                }

                await _channel.UploadChunk( new UploadChunkRequestDto()
                {
                    BinaryTransferUUID = identifier.BinaryTransferUUID,
                    ChunkIndex = i,
                    Payload = bytesToSend
                } );
            }

            return identifier.BinaryTransferUUID;
        }

        private IClientCallInfo CreateCallOptions()
        {
            var parameterIndex = _commandParameterIdentifier.LastIndexOf( "/Parameter/" );
            return _executionManager.CreateCallOptions( _commandParameterIdentifier.Substring( 0, parameterIndex ) );
        }

        /// <summary>
        /// Downloads the file with the given storage identifier to the given stream
        /// </summary>
        /// <param name="binaryIdentifier">The binary storage identifier</param>
        /// <param name="file">The stream in which to download the remote file</param>
        /// <returns>An awaitable task that completes when the download completes.</returns>
        public async Task Download( string binaryIdentifier, Stream file )
        {
            var data = await _channel.GetBinaryInfoAsync( new GetBinaryInfoRequestDto()
            {
                BinaryTransferUUID = binaryIdentifier
            } );
            var chunkCancellation = new CancellationTokenSource(data.LifetimeOfBinary.Extract(null));
            var chunkCount = (uint)(data.BinarySize + ChunkSize - 1) / ChunkSize;
            for(ulong i = 0; i < chunkCount; i++)
            {
                if (chunkCancellation.IsCancellationRequested)
                {
                    throw new BinaryDownloadFailedException("The server failed to respond within the specified lifetime of the binary", binaryIdentifier);
                }
                var chunk = await _channel.GetChunk( new GetChunkRequestDto()
                {
                    BinaryTransferUUID = binaryIdentifier,
                    Length = i < chunkCount - 1 ? ChunkSize : (uint)(data.BinarySize - i * ChunkSize),
                    Offset = i * ChunkSize
                } );
                chunkCancellation.CancelAfter(chunk.LifetimeOfBinary.Extract(null));
                await file.WriteAsync( chunk.Payload, 0, chunk.Payload.Length );
            }
        }

        /// <summary>
        /// Deletes the file with the given binary storage identifier
        /// </summary>
        /// <param name="binaryIdentifier">The binary storage identifier</param>
        /// <returns>An awaitable task that completes when the deletion is finished</returns>
        public async Task DeleteDownload( string binaryIdentifier )
        {
            await _channel.DeleteBinaryDownloadAsync( new DeleteBinaryRequestDto() { BinaryTransferUUID = binaryIdentifier }, CreateCallOptions() );
        }

        /// <inheritdoc />
        public FileInfo ResolveStoredBinaryFile( string identifier )
        {
            var tempPath = Path.GetTempFileName();
            using(var stream = File.OpenWrite( tempPath ))
            {
                Download( identifier, stream ).Wait();
            }
            return new FileInfo( tempPath );
        }

        /// <inheritdoc />
        public void Delete( string identifier )
        {
            DeleteDownload( identifier ).Wait();
        }
    }
}
