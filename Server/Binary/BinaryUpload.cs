using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.Server.Binary
{
    /// <summary>
    /// Helper class to implement upload of binary data
    /// </summary>
    public class BinaryUpload
    {
        private readonly IFileManagement _fileManagement;
        private readonly ISiLAServer _server;
        private readonly ConcurrentDictionary<string, FileUploadData> _openStreams = new ConcurrentDictionary<string, FileUploadData>();

        /// <summary>
        /// Create a binary upload for the given file management
        /// </summary>
        /// <param name="fileManagement">An object that handles the files locally</param>
        /// <param name="server">The parent server to validate requests</param>
        public BinaryUpload(IFileManagement fileManagement, ISiLAServer server)
        {
            _fileManagement = fileManagement;
            _server = server;
        }

        private Task<CreateBinaryResponseDto> CreateBinary(CreateBinaryRequestDto request, ServerCallContext context)
        {
            var commandIdentifier = GetCommandIdentifier( request.CommandIdentifier );
            var internalRequest = new InternalCreateBinaryRequest(commandIdentifier, request);
            return _server.InvokeCommand( CreateBinaryCore, internalRequest, context );
        }

        private CreateBinaryResponseDto CreateBinaryCore( InternalCreateBinaryRequest request)
        {
            var identifier = _fileManagement.CreateNew( (long)request.ActualRequest.BinarySize );
            var stream = _fileManagement.OpenWrite( identifier );
            var expiration = DateTimeOffset.Now.AddMinutes( 5 );
            _openStreams.TryAdd( identifier, new FileUploadData( stream, request.ActualRequest.ChunkCount )
            {
                ExpirationTime = expiration
            } );
            return new CreateBinaryResponseDto()
            {
                BinaryTransferUUID = identifier,
                LifetimeOfBinary = new TimestampDto( expiration )
            };
        }

        private string GetCommandIdentifier( string commandIdentifier )
        {
            if( commandIdentifier == null )
            {
                throw new ArgumentNullException(nameof(commandIdentifier));
            }
            var parameterSeparator = commandIdentifier.LastIndexOf( "/Parameter/", StringComparison.OrdinalIgnoreCase );
            if( parameterSeparator == -1 )
            {
                throw new ArgumentException($"The provided command identifier {commandIdentifier} is not a valid fully qualified command parameter.");
            }

            return commandIdentifier.Substring( 0, parameterSeparator );
        }

        private Task<EmptyRequest> DeleteBinary(DeleteBinaryRequestDto request, ServerCallContext context)
        {
            _fileManagement.Delete(request.BinaryTransferUUID, true);
            return Task.FromResult(EmptyRequest.Instance);
        }

        private async Task UploadChunk(IAsyncStreamReader<UploadChunkRequestDto> requestStream,IServerStreamWriter<UploadChunkResponseDto> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                await responseStream.WriteAsync(await UploadChunkCore(requestStream.Current, context));
            }
        }

        private async Task<UploadChunkResponseDto> UploadChunkCore(UploadChunkRequestDto request, ServerCallContext context)
        {
            if (_openStreams.TryGetValue(request.BinaryTransferUUID, out var uploadData))
            {
                if (request.ChunkIndex == uploadData.ChunkIndex)
                {
                    await uploadData.FileStream.WriteAsync(request.Payload, 0, request.Payload.Length);
                    uploadData.ChunkIndex++;
                    while (true)
                    {
                        KeyValuePair<uint, byte[]> cached;
                        lock (uploadData)
                        {
                            cached = uploadData.Cached.Count > 0 ? uploadData.Cached.First() : default;
                            if (cached.Key != uploadData.ChunkIndex)
                            {
                                break;
                            }
                        }
                        await uploadData.FileStream.WriteAsync(cached.Value, 0, cached.Value.Length);
                        uploadData.ChunkIndex++;
                    }
                    if (uploadData.ChunkIndex == uploadData.ChunkCount)
                    {
                        uploadData.FileStream.Close();
                        uploadData.FileStream.Dispose();
                        uploadData.Completion.Set();
                        _openStreams.TryRemove(request.BinaryTransferUUID, out _);
                    }
                }
                else if (request.ChunkIndex > uploadData.ChunkIndex)
                {
                    lock (uploadData)
                    {
                        if (uploadData.Cached.ContainsKey(request.ChunkIndex))
                        {
                            uploadData.Cached[request.ChunkIndex] = request.Payload;
                        }
                        else
                        {
                            uploadData.Cached.Add(request.ChunkIndex, request.Payload);
                        }
                    }
                }
                uploadData.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);
                return new UploadChunkResponseDto()
                {
                    LifetimeOfBinary = new TimestampDto(uploadData.ExpirationTime)
                };
            }
            else
            {
                throw new InvalidOperationException("Unknown file.");
            }
        }

        /// <summary>
        /// Checks whether the upload of the given identifier is complete
        /// </summary>
        /// <param name="binaryIdentifier">The binary storage identifier</param>
        /// <returns>True, if the upload of the file with the given identifier has been completed, otherwise false.</returns>
        public bool CheckUploadComplete(string binaryIdentifier)
        {
            return !_openStreams.ContainsKey(binaryIdentifier) && _fileManagement.Exists(binaryIdentifier);
        }

        /// <summary>
        /// Waits until the upload of the file with the given binary identifier is complete
        /// </summary>
        /// <param name="binaryIdentifier"></param>
        public void WaitUploadComplete(string binaryIdentifier)
        {
            if (_openStreams.TryGetValue(binaryIdentifier, out var uploadData))
            {
                uploadData.Completion.Wait();
            }
        }

        /// <summary>
        /// Creates the service definition to handle binary uploads
        /// </summary>
        /// <returns>A gRPC server service definition</returns>
        public ServerServiceDefinition CreateServiceDefinition()
        {
            ServerServiceDefinition.Builder builder = ServerServiceDefinition.CreateBuilder();
            builder.AddMethod(BinaryStorageConstants.DeleteBinaryUpload, DeleteBinary);
            builder.AddMethod(BinaryStorageConstants.CreateBinary, CreateBinary);
            builder.AddMethod(BinaryStorageConstants.UploadChunk, UploadChunk);
            return builder.Build();
        }

        private class InternalCreateBinaryRequest : ISilaRequestObject
        {
            public InternalCreateBinaryRequest( string commandIdentifier, CreateBinaryRequestDto actualRequest )
            {
                CommandIdentifier = commandIdentifier;
                ActualRequest = actualRequest;
            }

            public string GetValidationErrors()
            {
                return null;
            }

            public string CommandIdentifier
            {
                get;
            }

            public CreateBinaryRequestDto ActualRequest
            {
                get;
            }
        }
    }

}
