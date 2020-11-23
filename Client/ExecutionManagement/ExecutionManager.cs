using System.Collections.Generic;
using System.Linq;
using Grpc.Core;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.Client.ExecutionManagement
{
    internal class ExecutionManager : IClientExecutionManager
    {
        private readonly InterceptionRepository _interceptionRepository;
        private readonly GrpcServerData _serverData;

        public IBinaryStore DownloadBinaryStore { get; }

        public ExecutionManager( GrpcServerData serverData, IEnumerable<IClientRequestInterceptor> interceptors )
        {
            _interceptionRepository = new InterceptionRepository( interceptors );
            _serverData = serverData;

            FindAllMetadata();

            DownloadBinaryStore = new ClientBinaryStore( string.Empty, serverData.Channel, this );
        }

        private void FindAllMetadata()
        {
            var reader = new MetadataReader( _serverData.Channel );
            foreach(var feature in _serverData.Features)
            {
                foreach(var metadata in feature.Items.OfType<FeatureMetadata>())
                {
                    _interceptionRepository.RegisterAffected( feature.GetFullyQualifiedIdentifier( metadata ), reader.GetAffected( feature, metadata ) );
                }
            }
        }

        public IClientCallInfo CreateCallOptions( string commandIdentifier )
        {
            var interceptors = _interceptionRepository.GetInterceptorsForCommand( commandIdentifier );
            if(!interceptors.Any())
            {
                return DiscoveryExecutionManager.EmptyCallInfoInstance;
            }

            var metadata = new Dictionary<string, byte[]>();
            var interceptions = new List<IClientRequestInterception>();
            foreach(var interceptor in interceptors)
            {
                var intercept = interceptor.Intercept( _serverData, this, metadata );
                if(intercept != null)
                {
                    interceptions.Add( intercept );
                }
            }
            return new CallInfo( metadata, interceptions );
        }

        public IBinaryStore CreateBinaryStore( string commandParameterIdentifier )
        {
            return new ClientBinaryStore( commandParameterIdentifier, _serverData.Channel, this );
        }
    }
}
