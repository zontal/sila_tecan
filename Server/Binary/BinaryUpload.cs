using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.Binary;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server.Binary
{
    /// <summary>
    /// Helper class to implement upload of binary data
    /// </summary>
    public class BinaryUpload : IDisposable, IBinaryUploadHandler
#if ManagedGrpc
        , IServiceHandler<ISiLAServer>
#endif
    {
        private readonly IFileManagement _fileManagement;
        private readonly SiLAServer _server;
        private readonly ConcurrentDictionary<string, FileUploadData> _openStreams = new ConcurrentDictionary<string, FileUploadData>();
        private readonly Timer _cleanupTimer;

        /// <summary>
        /// Create a binary upload for the given file management
        /// </summary>
        /// <param name="fileManagement">An object that handles the files locally</param>
        /// <param name="server">The parent server to validate requests</param>
        public BinaryUpload(IFileManagement fileManagement, SiLAServer server)
        {
            _fileManagement = fileManagement;
            _server = server;
            _cleanupTimer = new Timer(Cleanup, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        private void Cleanup( object state )
        {
            Cleanup();
        }

        /// <summary>
        /// Closes all expired value streams
        /// </summary>
        public void Cleanup()
        {
            foreach( var fileUploadData in _openStreams )
            {
                if( fileUploadData.Value.ExpirationTime <= DateTimeOffset.Now )
                {
                    fileUploadData.Value.Completion.SetCanceled();
                    _fileManagement.Delete(fileUploadData.Key, true);
                    _openStreams.TryRemove( fileUploadData.Key, out _ );
                }
            }
        }

        /// <summary>
        /// Pauses the cleanup thread
        /// </summary>
        public void PauseCleanups()
        {
            _cleanupTimer.Change( Timeout.Infinite, Timeout.Infinite );
        }

        /// <summary>
        /// Resumes the cleanup thread
        /// </summary>
        /// <param name="interval"></param>
        public void ResumeCleanup(TimeSpan interval)
        {
            _cleanupTimer.Change( interval, interval );
        }

        private Task<CreateBinaryResponseDto> CreateBinary( CreateBinaryRequestDto request, ServerCallContext context )
        {
            try
            {
                var commandIdentifier = GetCommandIdentifier( request.CommandIdentifier );
                var internalRequest = new InternalCreateBinaryRequest( commandIdentifier, request );
                return _server.InvokeCommand( CreateBinaryCore, internalRequest, new MetadataRepository( context ) );
            }
            catch(InvalidBinaryTransferIdException transferException)
            {
                return Task.FromException<CreateBinaryResponseDto>( ServerErrorHandling.CreateBinaryError( new BinaryTransferError
                {
                    Type = BinaryTransferError.ErrorType.InvalidBinaryTransferUuid,
                    Message = transferException.Message
                } ) );
            }
            catch(Exception exception)
            {
                return Task.FromException<CreateBinaryResponseDto>( ServerErrorHandling.CreateBinaryError( new BinaryTransferError
                {
                    Type = BinaryTransferError.ErrorType.BinaryUploadFailed,
                    Message = exception.Message
                } ) );
            }
        }

        private CreateBinaryResponseDto CreateBinaryCore( InternalCreateBinaryRequest request )
        {
            string identifier = _fileManagement.CreateNew( (long)request.ActualRequest.BinarySize );
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

        private Task<EmptyRequest> DeleteBinary( DeleteBinaryRequestDto request, ServerCallContext context )
        {
            try
            {
                DeleteBinaryCore( request );
                return Task.FromResult( EmptyRequest.Instance );
            }
            catch(InvalidBinaryTransferIdException transferException)
            {
                return Task.FromException<EmptyRequest>( ServerErrorHandling.CreateBinaryError( new BinaryTransferError
                {
                    Type = BinaryTransferError.ErrorType.InvalidBinaryTransferUuid,
                    Message = transferException.Message
                } ) );
            }
            catch(Exception exception)
            {
                return Task.FromException<EmptyRequest>( ServerErrorHandling.CreateBinaryError( new BinaryTransferError
                {
                    Type = BinaryTransferError.ErrorType.BinaryUploadFailed,
                    Message = exception.Message
                } ) );
            }
        }

        private void DeleteBinaryCore( DeleteBinaryRequestDto request )
        {
            _fileManagement.Delete( request.BinaryTransferUUID, true );
        }

        private async Task UploadChunk(IAsyncStreamReader<UploadChunkRequestDto> requestStream,IServerStreamWriter<UploadChunkResponseDto> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                try
                {

                    await responseStream.WriteAsync(await UploadChunkCore(requestStream.Current, context));
                }
                catch(InvalidBinaryTransferIdException transferException)
                {
                    throw ServerErrorHandling.CreateBinaryError( new BinaryTransferError
                    {
                        Type = BinaryTransferError.ErrorType.InvalidBinaryTransferUuid,
                        Message = transferException.Message
                    } );
                }
                catch(Exception exception)
                {
                    throw ServerErrorHandling.CreateBinaryError( new BinaryTransferError
                    {
                        Type = BinaryTransferError.ErrorType.BinaryUploadFailed,
                        Message = exception.Message
                    } );
                }
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
                        uploadData.Completion.SetResult(null);
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
        /// Waits until the upload of the file with the given binary identifier is complete
        /// </summary>
        /// <param name="binaryIdentifier"></param>
        public void WaitUploadComplete(string binaryIdentifier)
        {
            if (_openStreams.TryGetValue(binaryIdentifier, out var uploadData))
            {
                uploadData.Completion.Task.Wait();
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
#if ManagedGrpc
        private Task<EmptyRequest> DeleteBinary( ISiLAServer server, DeleteBinaryRequestDto request, ServerCallContext context )
        {
            return DeleteBinary( request, context );
        }

        private Task<CreateBinaryResponseDto> CreateBinary( ISiLAServer server, CreateBinaryRequestDto request, ServerCallContext context )
        {
            return CreateBinary( request, context );
        }

        private Task UploadChunk( ISiLAServer server, IAsyncStreamReader<UploadChunkRequestDto> requestStream, IServerStreamWriter<UploadChunkResponseDto> responseStream, ServerCallContext context )
        {
            return UploadChunk( requestStream, responseStream, context );
        }

        /// <inheritdoc />
        public void Register( Grpc.AspNetCore.Server.Model.ServiceMethodProviderContext<ISiLAServer> context )
        {
            context.AddUnaryMethod( BinaryStorageConstants.DeleteBinaryUpload, new List<object>(), DeleteBinary );
            context.AddUnaryMethod( BinaryStorageConstants.CreateBinary, new List<object>(), CreateBinary );
            context.AddDuplexStreamingMethod( BinaryStorageConstants.UploadChunk, new List<object>(), UploadChunk );
        }
#endif

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

        /// <inheritdoc />
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }

        /// <summary>
        /// Deletes the upload with the given binary storage identifier
        /// </summary>
        /// <param name="binaryStorageIdentifier"></param>
        public void DeleteUpload( string binaryStorageIdentifier )
        {
            _openStreams.TryRemove( binaryStorageIdentifier, out _ );
            _fileManagement.Delete(binaryStorageIdentifier, true);
        }

        /// <inheritdoc />
        public async void CreateBinary( CreateBinaryUploadRequest createBinaryRequest, string requestUuid, IClientStreamWriter<SilaServerMessage> responseStream )
        {
            try
            {
                var request = createBinaryRequest.CreateBinaryRequest;
                var commandIdentifier = GetCommandIdentifier( request.CommandIdentifier );
                var internalRequest = new InternalCreateBinaryRequest( commandIdentifier, request );
                var response = await _server.InvokeCommand( CreateBinaryCore, internalRequest, createBinaryRequest );
                await responseStream.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = requestUuid,
                    CreateBinaryResponse = response
                } );
            }
            catch(InvalidBinaryTransferIdException transferException)
            {
                await responseStream.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = requestUuid,
                    BinaryError = new BinaryTransferError
                    {
                        Type = BinaryTransferError.ErrorType.InvalidBinaryTransferUuid,
                        Message = transferException.Message
                    }
                } );
            }
            catch(Exception exception)
            {
                await responseStream.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = requestUuid,
                    BinaryError = new BinaryTransferError
                    {
                        Type = BinaryTransferError.ErrorType.BinaryUploadFailed,
                        Message = exception.Message
                    }
                } );
            }
        }

        /// <inheritdoc />
        public async void UploadChunk( UploadChunkRequestDto uploadChunkRequest, string requestUuid, IClientStreamWriter<SilaServerMessage> responseStream )
        {
            try
            {
                var response = await UploadChunkCore( uploadChunkRequest, null );
                await responseStream.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = requestUuid,
                    UploadChunkResponse = response
                } );
            }
            catch(InvalidBinaryTransferIdException transferException)
            {
                await responseStream.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = requestUuid,
                    BinaryError = new BinaryTransferError
                    {
                        Type = BinaryTransferError.ErrorType.InvalidBinaryTransferUuid,
                        Message = transferException.Message
                    }
                } );
            }
            catch(Exception exception)
            {
                await responseStream.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = requestUuid,
                    BinaryError = new BinaryTransferError
                    {
                        Type = BinaryTransferError.ErrorType.BinaryUploadFailed,
                        Message = exception.Message
                    }
                } );
            }
        }

        /// <inheritdoc />
        public async void DeleteBinary( DeleteBinaryRequestDto deleteBinaryRequest, string requestUuid, IClientStreamWriter<SilaServerMessage> responseStream )
        {
            try
            {
                await DeleteBinary( deleteBinaryRequest, null );
                await responseStream.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = requestUuid,
                    DeleteBinaryResponse = new EmptyMessage()
                } );
            }
            catch(InvalidBinaryTransferIdException transferException)
            {
                await responseStream.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = requestUuid,
                    BinaryError = new BinaryTransferError
                    {
                        Type = BinaryTransferError.ErrorType.InvalidBinaryTransferUuid,
                        Message = transferException.Message
                    }
                } );
            }
            catch(Exception exception)
            {
                await responseStream.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = requestUuid,
                    BinaryError = new BinaryTransferError
                    {
                        Type = BinaryTransferError.ErrorType.BinaryUploadFailed,
                        Message = exception.Message
                    }
                } );
            }
        }
    }

}
