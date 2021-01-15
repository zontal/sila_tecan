using System;
using System.IO;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Binary
{
    /// <summary>
    /// A default implementation for a client binary data management
    /// </summary>
    public class ClientBinaryStore : ClientBase<ClientBinaryStore>, IBinaryStore
    {
        private const int ChunkSize = 1024 * 1024 * 2;
        private readonly string _commandParameterIdentifier;
        private readonly IClientExecutionManager _executionManager;

        /// <summary>
        /// Creates a new client binary store for the given command parameter identifier
        /// </summary>
        /// <param name="commandParameterIdentifier">A globally unique identifier for the command parameter or null, if the store is not associated to a command parameter</param>
        /// <param name="channel">The underlying channel</param>
        /// <param name="executionManager">The execution manager to handle requests to the given server</param>
        public ClientBinaryStore( string commandParameterIdentifier, Channel channel, IClientExecutionManager executionManager ) : base(channel)
        {
            _commandParameterIdentifier = commandParameterIdentifier;
            _executionManager = executionManager;
        }

        /// <summary>
        /// Creates a new client binary store for the given command parameter identifier
        /// </summary>
        /// <param name="commandParameterIdentifier">A globally unique identifier for the command parameter or null, if the store is not associated to a command parameter</param>
        /// <param name="callInvoker">The underlying channel</param>
        /// <param name="executionManager">The execution manager to handle requests to the given server</param>
        public ClientBinaryStore( string commandParameterIdentifier, CallInvoker callInvoker, IClientExecutionManager executionManager ) : base( callInvoker )
        {
            _commandParameterIdentifier = commandParameterIdentifier;
            _executionManager = executionManager;
        }

        /// <summary>
        /// Creates a new client binary store for the given command parameter identifier
        /// </summary>
        /// <param name="commandParameterIdentifier">A globally unique identifier for the command parameter or null, if the store is not associated to a command parameter</param>
        /// <param name="configuration">The updated configuration</param>
        /// <param name="executionManager">The execution manager to handle requests to the given server</param>
        protected ClientBinaryStore(string commandParameterIdentifier, ClientBaseConfiguration configuration, IClientExecutionManager executionManager ) : base(configuration)
        {
            _commandParameterIdentifier = commandParameterIdentifier;
            _executionManager = executionManager;
        }

        /// <inheritdoc />
        public Stream ResolveStoredBinary(string identifier)
        {
            var ms = new MemoryStream();
            Download(identifier, ms).Wait();
            ms.Position = 0;
            return ms;
        }

        /// <inheritdoc />
        public bool ShouldStoreBinary(long length)
        {
            return length > 200 * 1024;
        }

        /// <inheritdoc />
        public string StoreBinary(Stream data)
        {
            var task = Upload(data);
            task.Wait();
            return task.Result;
        }

        /// <inheritdoc />
        public string StoreBinary(FileInfo file)
        {
            using (var stream = file.OpenRead())
            {
                return StoreBinary(stream);
            }
        }

        private AsyncUnaryCall<CreateBinaryResponseDto> CreateBinaryAsync(CreateBinaryRequestDto request, CallOptions options)
        {
            return CallInvoker.AsyncUnaryCall(BinaryStorageConstants.CreateBinary, null, options, request);
        }

        private AsyncUnaryCall<EmptyRequest> DeleteBinaryUploadAsync(DeleteBinaryRequestDto request, CallOptions options)
        {
            return CallInvoker.AsyncUnaryCall(BinaryStorageConstants.DeleteBinaryUpload, null, options, request);
        }

        private AsyncDuplexStreamingCall<UploadChunkRequestDto, UploadChunkResponseDto> UploadChunk(CallOptions options)
        {
            return CallInvoker.AsyncDuplexStreamingCall(BinaryStorageConstants.UploadChunk, null, options);
        }

        /// <summary>
        /// Uploads the given file
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <returns>An awaitable task that returns the storage identifier</returns>
        public async Task<string> Upload(Stream file)
        {
            var chunkCount = (uint) ((file.Length + ChunkSize - 1) / ChunkSize);
            var options = CreateCallOptions( true );
            var identifier = await CreateBinaryAsync(new CreateBinaryRequestDto()
            {
                BinarySize = (ulong) file.Length,
                ChunkCount = chunkCount,
                CommandIdentifier = _commandParameterIdentifier
            }, options);
            var uploadChannel = UploadChunk(new CallOptions());
            var bytes = new byte[ChunkSize];
            for (uint i = 0; i < chunkCount; i++)
            {
                var bytesRead = await file.ReadAsync(bytes, 0, ChunkSize);
                byte[] bytesToSend;
                if (bytesRead == ChunkSize)
                {
                    bytesToSend = bytes;
                }
                else
                {
                    bytesToSend = new byte[bytesRead];
                    Array.Copy(bytes, bytesToSend, bytesRead);
                }

                await uploadChannel.RequestStream.WriteAsync(new UploadChunkRequestDto()
                {
                    BinaryTransferUUID = identifier.BinaryTransferUUID,
                    ChunkIndex = i,
                    Payload = bytesToSend
                });
            }
            await uploadChannel.RequestStream.CompleteAsync();

            return identifier.BinaryTransferUUID;
        }

        private CallOptions CreateCallOptions(bool useExecutionManager)
        {
            if (useExecutionManager && _commandParameterIdentifier != null)
            {
                var parameterIndex = _commandParameterIdentifier.LastIndexOf( "/Parameter/" );
                return _executionManager.CreateCallOptions( _commandParameterIdentifier.Substring( 0, parameterIndex ) ).ToCallOptions();
            }
            else
            {
                return new CallOptions();
            }
        }

        private AsyncUnaryCall<GetBinaryInfoResponseDto> GetBinaryInfoAsync(GetBinaryInfoRequestDto request, CallOptions options)
        {
            return CallInvoker.AsyncUnaryCall(BinaryStorageConstants.GetBinaryInfo, null, options, request);
        }

        private AsyncDuplexStreamingCall<GetChunkRequestDto, GetChunkResponseDto> GetChunk(CallOptions options)
        {
            return CallInvoker.AsyncDuplexStreamingCall(BinaryStorageConstants.GetChunk, null, options);
        }

        private AsyncUnaryCall<EmptyRequest> DeleteBinaryDownloadAsync(DeleteBinaryRequestDto request, CallOptions options)
        {
            return CallInvoker.AsyncUnaryCall(BinaryStorageConstants.DeleteBinaryDownload, null, options, request);
        }

        /// <summary>
        /// Downloads the file with the given storage identifier to the given stream
        /// </summary>
        /// <param name="binaryIdentifier">The binary storage identifier</param>
        /// <param name="file">The stream in which to download the remote file</param>
        /// <returns>An awaitable task that completes when the download completes.</returns>
        public async Task Download(string binaryIdentifier, Stream file)
        {
            var data = await GetBinaryInfoAsync(new GetBinaryInfoRequestDto()
            {
                BinaryTransferUUID = binaryIdentifier
            }, CreateCallOptions(false));
            var getChunk = GetChunk(CreateCallOptions(false));
            var chunkCount = (uint) (data.BinarySize + ChunkSize - 1) / ChunkSize;
            for (ulong i = 0; i < chunkCount; i++)
            {
                await getChunk.RequestStream.WriteAsync(new GetChunkRequestDto()
                {
                    BinaryTransferUUID = binaryIdentifier,
                    Length = ChunkSize,
                    Offset = i * ChunkSize
                });
                await getChunk.ResponseStream.MoveNext( default );
                var chunk = getChunk.ResponseStream.Current;
                await file.WriteAsync(chunk.Payload, 0, chunk.Payload.Length);
            }

            await getChunk.RequestStream.CompleteAsync();
        }

        /// <summary>
        /// Deletes the file with the given binary storage identifier
        /// </summary>
        /// <param name="binaryIdentifier">The binary storage identifier</param>
        /// <returns>An awaitable task that completes when the deletion is finished</returns>
        public async Task DeleteDownload(string binaryIdentifier)
        {
            await DeleteBinaryDownloadAsync(new DeleteBinaryRequestDto() { BinaryTransferUUID = binaryIdentifier }, CreateCallOptions(true));
        }

        /// <summary>
        /// Creates a new instance with the given new client configuration
        /// </summary>
        /// <param name="configuration">The new client configuration</param>
        /// <returns>A new instance of the binary store</returns>
        protected override ClientBinaryStore NewInstance(ClientBaseConfiguration configuration)
        {
            return new ClientBinaryStore( _commandParameterIdentifier, configuration, _executionManager);
        }

        /// <inheritdoc />
        public FileInfo ResolveStoredBinaryFile( string identifier )
        {
            var tempPath = Path.GetTempFileName();
            using( var stream = File.OpenWrite(tempPath) )
            {
                Download( identifier, stream ).Wait();
            }
            return new FileInfo(tempPath);
        }

        /// <inheritdoc />
        public void Delete( string identifier )
        {
            DeleteDownload( identifier ).Wait();
        }
    }
}
