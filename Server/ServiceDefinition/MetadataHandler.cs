using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class MetadataHandler : IRequestHandler
    {
        private readonly SiLAServer _server;
        private readonly IRequestInterceptor _requestInterceptor;

        public MetadataHandler( SiLAServer server, IRequestInterceptor requestInterceptor )
        {
            _server = server;
            _requestInterceptor = requestInterceptor;
        }

        public Task<List<StringDto>> GetAffectedBy( EmptyRequest request, ServerCallContext context )
        {
            List<StringDto> affected = DtoExtensions.Encapsulate( _server.Pipeline.GetAffectedOfMetadata( _requestInterceptor ), StringDto.Create, null );

            return Task.FromResult( affected );
        }

        public async void HandleRequest( SilaClientMessage message, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher )
        {
            if(message.Type != SilaClientMessageType.MetadataRequest)
            {
                throw new ArgumentException( $"The message type {message.Type} is not supported.", nameof( message ) );
            }

            await responseWriter.WriteAsync( new SilaServerMessage
            {
                RequestUuid = message.RequestUuid,
                MetadataResponse = new GetFCPAffectedByMetadataResponse
                {
                    AffectedCalls = _server.Pipeline.GetAffectedOfMetadata( _requestInterceptor ).ToList()
                }
            } );
        }
    }
}
