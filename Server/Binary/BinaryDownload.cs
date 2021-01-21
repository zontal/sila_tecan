using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.Binary;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server.Binary
{
    /// <summary>
    /// A helper class to implement binary download functionality based on a given file management implementation
    /// </summary>
    public class BinaryDownload : IBinaryDownloadHandler
    {
        private readonly Timer _disposeTimer;
        private readonly IFileManagement _server;
        private readonly ConcurrentDictionary<string, (Stream stream, DateTimeOffset expiration)> _openStreams = new ConcurrentDictionary<string, (Stream, DateTimeOffset)>();

        /// <summary>
        /// Create a new binary download utility for the given local file management
        /// </summary>
        /// <param name="server"></param>
        public BinaryDownload(IFileManagement server)
        {
            _server = server;
            _disposeTimer = new Timer(DisposeExpiredStreams);
            _disposeTimer.Change(10000, 10000);
        }

        private void DisposeExpiredStreams(object state)
        {
            foreach (var item in _openStreams.ToArray())
            {
                if (item.Value.expiration < DateTimeOffset.Now)
                {
                    if (item.Value.stream != null)
                    {
                        item.Value.stream.Close();
                        item.Value.stream.Dispose();
                    }
                    _openStreams.TryRemove(item.Key, out _);
                    _server.Delete(item.Key, false);
                }
            }
        }

        private Task<EmptyRequest> DeleteBinary(DeleteBinaryRequestDto request, ServerCallContext context)
        {
            _server.Delete(request.BinaryTransferUUID, false);
            return Task.FromResult(EmptyRequest.Instance);
        }

        private Task<GetBinaryInfoResponseDto> GetBinaryInfo(GetBinaryInfoRequestDto request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult( GetBinaryInfoCore( request ) );
            }
            catch (InvalidBinaryTransferIdException transferException)
            {
                return Task.FromException<GetBinaryInfoResponseDto>( ServerErrorHandling.CreateBinaryError( new BinaryTransferError
                {
                    Type = BinaryTransferError.ErrorType.InvalidBinaryTransferUuid,
                    Message = transferException.Message
                } ) );
            }
            catch (Exception exception)
            {
                return Task.FromException<GetBinaryInfoResponseDto>( ServerErrorHandling.CreateBinaryError( new BinaryTransferError
                {
                    Type = BinaryTransferError.ErrorType.BinaryDownloadFailed,
                    Message = exception.Message
                } ) );
            }
        }

        private GetBinaryInfoResponseDto GetBinaryInfoCore( GetBinaryInfoRequestDto request )
        {
            (Stream stream, DateTimeOffset expiration) fileData;
            if(!_openStreams.TryGetValue( request.BinaryTransferUUID, out fileData ) || fileData.stream == null)
            {
                fileData = OpenFile( request.BinaryTransferUUID, fileData.expiration );
            }
            return new GetBinaryInfoResponseDto()
            {
                BinarySize = (ulong)fileData.stream.Length,
                LifetimeOfBinary = new TimestampDto( fileData.expiration )
            };
        }

        private (Stream stream, DateTimeOffset expiration) OpenFile(string binaryIdentifier, DateTimeOffset expiration)
        {
            if (expiration == default(DateTimeOffset))
            {
                expiration = DateTimeOffset.Now.AddMinutes(5);
            }
            if (_server.Exists(binaryIdentifier))
            {
                var fileData = (_server.OpenRead(binaryIdentifier), expiration);
                _openStreams.TryAdd(binaryIdentifier, fileData);
                return fileData;
            }
            else
            {
                throw new InvalidBinaryTransferIdException( "The specified file does not exist", binaryIdentifier );
            }
        }

        private async Task GetChunk(IAsyncStreamReader<GetChunkRequestDto> requestStream, IServerStreamWriter<GetChunkResponseDto> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext(context.CancellationToken))
            {
                try
                {
                    await responseStream.WriteAsync(await GetChunkCore(requestStream.Current, context));
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
                        Type = BinaryTransferError.ErrorType.BinaryDownloadFailed,
                        Message = exception.Message
                    } );
                }
            }
        }

        private async Task<GetChunkResponseDto> GetChunkCore( GetChunkRequestDto request, ServerCallContext context )
        {
            if(!_openStreams.TryGetValue( request.BinaryTransferUUID, out var fileData ) || fileData.stream == null)
            {
                fileData = OpenFile( request.BinaryTransferUUID, fileData.expiration );
            }
            fileData.stream.Position = (long)request.Offset;
            var newExpiration = DateTimeOffset.Now.AddMinutes( 5 );
            _openStreams.TryUpdate( request.BinaryTransferUUID, (fileData.stream, newExpiration), fileData );
            var bytes = new byte[request.Length];
            var bytesRead = await fileData.stream.ReadAsync( bytes, 0, (int)request.Length );
            var bytesToSend = bytes;
            if(bytesRead != request.Length)
            {
                bytesToSend = new byte[bytesRead];
                Array.Copy( bytes, bytesToSend, bytesRead );
            }
            return new GetChunkResponseDto()
            {
                Payload = bytesToSend,
                BinaryTransferUUID = request.BinaryTransferUUID,
                LifetimeOfBinary = new TimestampDto( newExpiration ),
            };
        }

        /// <summary>
        /// Creates the service definition to handle binary downloads
        /// </summary>
        /// <returns>A gRPC server service definition</returns>
        public ServerServiceDefinition CreateServiceDefinition()
        {
            ServerServiceDefinition.Builder builder = ServerServiceDefinition.CreateBuilder();
            builder.AddMethod(BinaryStorageConstants.DeleteBinaryDownload, DeleteBinary);
            builder.AddMethod(BinaryStorageConstants.GetBinaryInfo, GetBinaryInfo);
            builder.AddMethod(BinaryStorageConstants.GetChunk, GetChunk);
            return builder.Build();
        }

        /// <inheritdoc />
        public async void GetBinaryInfo( GetBinaryInfoRequestDto getBinaryInfoRequest, string requestUuid, IClientStreamWriter<SilaServerMessage> responseStream )
        {
            try
            {
                var response = GetBinaryInfoCore( getBinaryInfoRequest );
                await responseStream.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = requestUuid,
                    GetBinaryResponse = response
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
                        Type = BinaryTransferError.ErrorType.BinaryDownloadFailed,
                        Message = exception.Message
                    }
                } );
            }
        }

        /// <inheritdoc />
        public async void GetChunk( GetChunkRequestDto getChunkRequest, string requestUuid, IClientStreamWriter<SilaServerMessage> responseStream )
        {
            try
            {

                var response = await GetChunkCore( getChunkRequest, null );
                await responseStream.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = requestUuid,
                    GetChunkResponse = response
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
                        Type = BinaryTransferError.ErrorType.BinaryDownloadFailed,
                        Message = exception.Message
                    }
                } );
            }
        }
    }

}
