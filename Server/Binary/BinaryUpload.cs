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

    public class BinaryUpload
    {
        private IFileManagement _server;
        private ConcurrentDictionary<string, FileUploadData> _openStreams = new ConcurrentDictionary<string, FileUploadData>();

        public BinaryUpload(IFileManagement server)
        {
            _server = server;
        }

        private string ResolveTempFileName(string arg)
        {
            return Path.Combine(Path.GetTempPath(), arg);
        }

        public Task<CreateBinaryResponseDto> CreateBinary(CreateBinaryRequestDto request, ServerCallContext context)
        {
            var identifier = _server.CreateNew((long) request.BinarySize);
            var stream = _server.OpenWrite(identifier);
            var expiration = DateTimeOffset.Now.AddMinutes(5);
            _openStreams.TryAdd(identifier, new FileUploadData(stream, request.ChunkCount)
            {
                ExpirationTime = expiration
            });
            return Task.FromResult(new CreateBinaryResponseDto()
            {
                BinaryTransferUUID = identifier,
                LifetimeOfBinary = new TimestampDto(expiration)
            });
        }

        public Task<EmptyRequest> DeleteBinary(DeleteBinaryRequestDto request, ServerCallContext context)
        {
            _server.Delete(request.BinaryTransferUUID, true);
            return Task.FromResult(EmptyRequest.Instance);
        }

        public async Task UploadChunk(IAsyncStreamReader<UploadChunkRequestDto> requestStream,IServerStreamWriter<UploadChunkResponseDto> responseStream, ServerCallContext context)
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

        public bool CheckUploadComplete(string binaryIdentifier)
        {
            return !_openStreams.ContainsKey(binaryIdentifier) && _server.Exists(binaryIdentifier);
        }

        public ServerServiceDefinition CreateServiceDefinition()
        {
            ServerServiceDefinition.Builder builder = ServerServiceDefinition.CreateBuilder();
            builder.AddMethod(BinaryStorageConstants.DeleteBinaryUpload, DeleteBinary);
            builder.AddMethod(BinaryStorageConstants.CreateBinary, CreateBinary);
            builder.AddMethod(BinaryStorageConstants.UploadChunk, UploadChunk);
            return builder.Build();
        }
    }

}
