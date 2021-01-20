using Grpc.Core;
using System;
using System.Threading;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.ServerPooling
{
    internal interface IRequestHandler
    {
        void HandleRequest( SilaClientMessage message, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher );
    }

    internal interface ICommandRequestHandler : IRequestHandler
    {
        void HandleExecutionStateSubscription( string requestUuid, ILoggingChannel loggingChannel, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher );
        void HandleIntermediatesSubscription( string requestUuid, ILoggingChannel loggingChannel, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher );
        void HandleResultRequest( string requestUuid, ILoggingChannel loggingChannel, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher );
    }

    internal interface IIntermediateRequestHandler
    {
        void HandleIntermediatesSubscription( string requestUuid, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher );
    }

    internal interface IBinaryUploadHandler
    {
        void CreateBinary( CreateBinaryUploadRequest createBinaryRequest, string requestUuid, IClientStreamWriter<SilaServerMessage> responseStream );
        void UploadChunk( UploadChunkRequestDto uploadChunkRequest, string requestUuid, IClientStreamWriter<SilaServerMessage> responseStream );
        void DeleteBinary( DeleteBinaryUploadRequest deleteBinaryRequest, string requestUuid, IClientStreamWriter<SilaServerMessage> responseStream );
    }

    internal interface IBinaryDownloadHandler
    {
        void GetBinaryInfo( GetBinaryInfoRequestDto getBinaryInfoRequest, string requestUuid, IClientStreamWriter<SilaServerMessage> responseStream );
        void GetChunk( GetChunkRequestDto getChunkRequest, string requestUuid, IClientStreamWriter<SilaServerMessage> responseStream );
    }
}
