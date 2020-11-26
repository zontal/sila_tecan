using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class MetadataHandler
    {
        private readonly SiLAServer _server;
        private readonly IRequestInterceptor _requestInterceptor;

        public MetadataHandler(SiLAServer server, IRequestInterceptor requestInterceptor)
        {
            _server = server;
            _requestInterceptor = requestInterceptor;
        }

        public Task<List<StringDto>> GetAffectedBy( EmptyRequest request, Grpc.Core.ServerCallContext context )
        {
            var affected = new List<StringDto>();

            foreach(var provider in _server.Features)
            {
                var feature = provider.FeatureDefinition;
                if(_requestInterceptor.AppliesToCommands)
                {
                    foreach(var command in feature.Items.OfType<FeatureCommand>())
                    {
                        var identifier = feature.GetFullyQualifiedIdentifier( command );
                        if(_requestInterceptor.IsInterceptRequired( feature, identifier ))
                        {
                            affected.Add( new StringDto( identifier ) );
                        }
                    }
                }
                if(_requestInterceptor.AppliesToProperties)
                {
                    foreach(var property in feature.Items.OfType<FeatureProperty>())
                    {
                        var identifier = feature.GetFullyQualifiedIdentifier( property );
                        if(_requestInterceptor.IsInterceptRequired( feature, identifier ))
                        {
                            affected.Add( new StringDto( identifier ) );
                        }
                    }
                }
            }

            return Task.FromResult( affected );
        }


    }
}
