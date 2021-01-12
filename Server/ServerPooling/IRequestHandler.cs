using Grpc.Core;
using System;
using System.Threading;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.ServerPooling
{
    internal interface IRequestHandler
    {
        void HandleRequest( SilaClientMessage message, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher );
    }

    internal interface ICommandRequestHandler : IRequestHandler
    {
        void HandleExecutionStateSubscription( string requestUuid, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher );
        void HandleIntermediatesSubscription( string requestUuid, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher );
        void HandleResultRequest( string requestUuid, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher );
    }

    internal interface IIntermediateRequestHandler
    {
        void HandleIntermediatesSubscription( string requestUuid, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher );
    }
}
