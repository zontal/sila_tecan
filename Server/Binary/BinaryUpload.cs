using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
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
        , IServiceHandler<ISiLAServer, Grpc.AspNetCore.Server.Model.ServiceMethodProviderContext<ISiLAServer>>
#endif
    {
        private readonly IFileManagement _fileManagement;
        private readonly SiLAServer _server;
        private readonly ConcurrentDictionary<string, FileUploadData> _openStreams = new ConcurrentDictionary<string, FileUploadData>();
        private readonly Timer _cleanupTimer;
        private readonly ILog _log = LogManager.GetLogger( typeof( BinaryUpload ) );

        private const double WaitTimeInMinutes = 1;

        private readonly HashSet<string> _allowedCommandParameters = new HashSet<string>();

        /// <summary>
        /// Create a binary upload for the given file management
        /// </summary>
        /// <param name="fileManagement">An object that handles the files locally</param>
        /// <param name="server">The parent server to validate requests</param>
        public BinaryUpload( IFileManagement fileManagement, SiLAServer server )
        {
            _fileManagement = fileManagement;
            _server = server;
            _cleanupTimer = new Timer( Cleanup, null, TimeSpan.FromMinutes( WaitTimeInMinutes ), TimeSpan.FromMinutes( 5 ) );
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
            foreach(var fileUploadData in _openStreams)
            {
                if(fileUploadData.Value.ExpirationTime <= DateTimeOffset.Now)
                {
                    try
                    {
                        if(!fileUploadData.Value.Completion.Task.IsCompleted)
                        {
                            fileUploadData.Value.Completion.SetCanceled();
                        }
                        if (_openStreams.TryRemove(fileUploadData.Key, out var stream))
                        {
                            stream.FileStream.Dispose();
                        }
                        _fileManagement.Delete( fileUploadData.Key, true );
                    }
                    catch(Exception ex)
                    {
                        _log.Error( $"Error cleaning up stored binary {fileUploadData.Key}", ex );
                    }
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
        public void ResumeCleanup( TimeSpan interval )
        {
            _cleanupTimer.Change( interval, interval );
        }

        private Task<CreateBinaryResponseDto> CreateBinary( CreateBinaryRequestDto request, ServerCallContext context )
        {
            try
            {
                lock(_allowedCommandParameters)
                {
                    if(!_allowedCommandParameters.Contains( request.CommandIdentifier ))
                    {
                        throw new InvalidOperationException( $"{request.CommandIdentifier} does not support parameters" );
                    }
                }
                var commandIdentifier = GetCommandIdentifier( request.CommandIdentifier );
                var internalRequest = new InternalCreateBinaryRequest( commandIdentifier, request );
                return _server.InvokeCommand( CreateBinaryCore, internalRequest, new MetadataRepository( context ) );
            }
            catch(InvalidBinaryTransferIdException transferException)
            {
                return Task.FromException<CreateBinaryResponseDto>( ServerErrorHandling.Instance.CreateBinaryError( new BinaryTransferError
                {
                    Type = BinaryTransferError.ErrorType.InvalidBinaryTransferUuid,
                    Message = transferException.Message
                } ) );
            }
            catch(Exception exception)
            {
                return Task.FromException<CreateBinaryResponseDto>( ServerErrorHandling.Instance.CreateBinaryError( new BinaryTransferError
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
            var expiration = DateTimeOffset.Now.AddMinutes( WaitTimeInMinutes );
            _openStreams.TryAdd( identifier, new FileUploadData( stream, request.ActualRequest.ChunkCount, (long)request.ActualRequest.BinarySize )
            {
                ExpirationTime = expiration
            } );
            return new CreateBinaryResponseDto()
            {
                BinaryTransferUUID = identifier,
                LifetimeOfBinary = new DurationDto( TimeSpan.FromMinutes(5) )
            };
        }

        private string GetCommandIdentifier( string commandIdentifier )
        {
            if(commandIdentifier == null)
            {
                throw new ArgumentNullException( nameof( commandIdentifier ) );
            }
            var parameterSeparator = commandIdentifier.LastIndexOf( "/Parameter/", StringComparison.OrdinalIgnoreCase );
            if(parameterSeparator == -1)
            {
                throw new ArgumentException( $"The provided command identifier {commandIdentifier} is not a valid fully qualified command parameter." );
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
                return Task.FromException<EmptyRequest>( ServerErrorHandling.Instance.CreateBinaryError( new BinaryTransferError
                {
                    Type = BinaryTransferError.ErrorType.InvalidBinaryTransferUuid,
                    Message = transferException.Message
                } ) );
            }
            catch(Exception exception)
            {
                return Task.FromException<EmptyRequest>( ServerErrorHandling.Instance.CreateBinaryError( new BinaryTransferError
                {
                    Type = BinaryTransferError.ErrorType.BinaryUploadFailed,
                    Message = exception.Message
                } ) );
            }
        }

        private void DeleteBinaryCore( DeleteBinaryRequestDto request )
        {
            DeleteUpload(request.BinaryTransferUUID);
        }

        private async Task UploadChunk( IAsyncStreamReader<UploadChunkRequestDto> requestStream, IServerStreamWriter<UploadChunkResponseDto> responseStream, ServerCallContext context )
        {
            while(await requestStream.MoveNext())
            {
                try
                {
                    await responseStream.WriteAsync( await UploadChunkCore( requestStream.Current, context ) );
                }
                catch(InvalidBinaryTransferIdException transferException)
                {
                    throw ServerErrorHandling.Instance.CreateBinaryError( new BinaryTransferError
                    {
                        Type = BinaryTransferError.ErrorType.InvalidBinaryTransferUuid,
                        Message = transferException.Message
                    } );
                }
                catch(Exception exception)
                {
                    throw ServerErrorHandling.Instance.CreateBinaryError( new BinaryTransferError
                    {
                        Type = BinaryTransferError.ErrorType.BinaryUploadFailed,
                        Message = exception.Message
                    } );
                }
            }
        }

        private async Task<UploadChunkResponseDto> UploadChunkCore( UploadChunkRequestDto request, ServerCallContext context )
        {
            if(_openStreams.TryGetValue( request.BinaryTransferUUID, out var uploadData ))
            {
                if(request.ChunkIndex >= uploadData.ChunkCount)
                {
                    throw new InvalidOperationException( "Chunk index is invalid" );
                }
                if(uploadData.Completion.Task.IsCompleted)
                {
                    throw new InvalidOperationException( $"The binary {request.BinaryTransferUUID} has already been uploaded" );
                }
                if(request.ChunkIndex < uploadData.ChunkIndex)
                {
                    throw new InvalidOperationException( $"The chunk {request.ChunkIndex} was already uploaded" );
                }
                if(request.Payload != null && request.Payload.Length > BinaryDownload.MaxChunkSize)
                {
                    throw new InvalidOperationException( "The request is larger than the allowed maximum chunk size." );
                }
                if(request.ChunkIndex == uploadData.ChunkIndex)
                {
                    if(request.Payload != null && request.Payload.Length > 0)
                    {
                        await uploadData.FileStream.WriteAsync( request.Payload, 0, request.Payload.Length );
                    }
                    uploadData.ChunkIndex++;
                    while(true)
                    {
                        byte[] cached;
                        lock(uploadData)
                        {
                            if(!uploadData.Cached.TryGetValue( uploadData.ChunkIndex, out cached ))
                            {
                                break;
                            }
                        }
                        if(cached != null && cached.Length > 0)
                        {
                            await uploadData.FileStream.WriteAsync( cached, 0, cached.Length );
                        }
                        uploadData.ChunkIndex++;
                    }

                    if(uploadData.FileStream.Length > uploadData.Size)
                    {
                        uploadData.FileStream.Close();
                        uploadData.FileStream.Dispose();

                        var exception = new InvalidOperationException( $"Expected {uploadData.Size} bytes but received {uploadData.FileStream.Length} bytes" );
                        uploadData.Completion.SetException( exception );
                        throw exception;
                    }
                    if(uploadData.ChunkIndex == uploadData.ChunkCount)
                    {
                        uploadData.FileStream.Close();
                        uploadData.FileStream.Dispose();
                        uploadData.Completion.SetResult( null );
                    }
                }
                else if(request.ChunkIndex > uploadData.ChunkIndex)
                {
                    lock(uploadData)
                    {
                        if(uploadData.Cached.ContainsKey( request.ChunkIndex ))
                        {
                            uploadData.Cached[request.ChunkIndex] = request.Payload;
                        }
                        else
                        {
                            uploadData.Cached.Add( request.ChunkIndex, request.Payload );
                        }
                    }
                }
                uploadData.ExpirationTime = DateTimeOffset.Now.AddMinutes( WaitTimeInMinutes );
                return new UploadChunkResponseDto()
                {
                    LifetimeOfBinary = new DurationDto(TimeSpan.FromMinutes(WaitTimeInMinutes)),
                    BinaryTransferUUID = request.BinaryTransferUUID,
                    ChunkIndex = request.ChunkIndex
                };
            }
            else
            {
                throw new InvalidBinaryTransferIdException( "Unknown file.", request.BinaryTransferUUID );
            }
        }

        /// <summary>
        /// Waits until the upload of the file with the given binary identifier is complete
        /// </summary>
        /// <param name="binaryIdentifier"></param>
        public bool IsUploadCompleted( string binaryIdentifier )
        {
            if(_openStreams.TryGetValue( binaryIdentifier, out var uploadData ))
            {
                // allow the binary upload to complete within 1s
                return uploadData.Completion.Task.Wait( 1000 );
            }
            return _fileManagement.Exists( binaryIdentifier );
        }

        /// <summary>
        /// Waits until the upload of the file with the given binary identifier is complete
        /// </summary>
        /// <param name="binaryIdentifier"></param>
        public void WaitUploadComplete( string binaryIdentifier )
        {
            if(_openStreams.TryGetValue( binaryIdentifier, out var uploadData ) && !uploadData.Completion.Task.IsCompleted)
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
            builder.AddMethod( BinaryStorageConstants.DeleteBinaryUpload, DeleteBinary );
            builder.AddMethod( BinaryStorageConstants.CreateBinary, CreateBinary );
            builder.AddMethod( BinaryStorageConstants.UploadChunk, UploadChunk );
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

            public void Validate()
            {
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
            if (_openStreams.TryRemove( binaryStorageIdentifier, out var openStream ))
            {
                openStream.FileStream.Dispose();
            }
            _fileManagement.Delete( binaryStorageIdentifier, true );
        }

        /// <inheritdoc />
        public async void CreateBinary( CreateBinaryUploadRequest createBinaryRequest, string requestUuid, IClientStreamWriter<SilaServerMessage> responseStream )
        {
            try
            {
                lock(_allowedCommandParameters)
                {
                    if(!_allowedCommandParameters.Contains( createBinaryRequest.CreateBinaryRequest.CommandIdentifier ))
                    {
                        throw new InvalidOperationException( $"{createBinaryRequest.CreateBinaryRequest.CommandIdentifier} does not accept binaries" );
                    }
                }
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

        internal void RegisterCommandParameter( string commandParameter )
        {
            lock(_allowedCommandParameters)
            {
                _allowedCommandParameters.Add( commandParameter );
            }
        }
    }

}
