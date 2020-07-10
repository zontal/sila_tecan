using System.Collections.Generic;
using System.Linq;
using Grpc.Core;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.Client.ExecutionManagement
{
    internal class ExecutionManager : ClientBinaryStore, IClientExecutionManager
    {
        private readonly InterceptionRepository _interceptionRepository;
        private readonly ServerData _serverData;

        public IBinaryStore DownloadBinaryStore => this;

        public ExecutionManager( ServerData serverData, IEnumerable<IRequestInterceptor> interceptors ) : base( "", serverData.Channel )
        {
            _interceptionRepository = new InterceptionRepository( interceptors );
            _serverData = serverData;

            FindAllMetadata();
        }

        private void FindAllMetadata()
        {
            var reader = new MetadataReader( CallInvoker );
            foreach(var feature in _serverData.Features)
            {
                foreach(var metadata in feature.Items.OfType<FeatureMetadata>())
                {
                    _interceptionRepository.RegisterAffected( feature.GetFullyQualifiedIdentifier( metadata ), reader.GetAffected( feature, metadata ) );
                }
            }
        }

        protected ExecutionManager( ClientBaseConfiguration configuration, InterceptionRepository interceptionRepository, ServerData server ) : base( "", configuration )
        {
            _interceptionRepository = interceptionRepository;
            _serverData = server;
        }

        public IClientCallInfo CreateCallOptions( string commandIdentifier )
        {
            var interceptors = _interceptionRepository.GetInterceptorsForCommand( commandIdentifier );
            if(!interceptors.Any())
            {
                return CallInfo.NoMetadata;
            }

            var metadata = new Metadata();
            var interceptions = new List<IRequestInterception>();
            foreach(var interceptor in interceptors)
            {
                var intercept = interceptor.Intercept( _serverData, this, metadata );
                if(intercept != null)
                {
                    interceptions.Add( intercept );
                }
            }
            return new CallInfo( new CallOptions( headers: metadata ), interceptions );
        }

        protected override ClientBinaryStore NewInstance( ClientBaseConfiguration configuration )
        {
            return new ExecutionManager( configuration, _interceptionRepository, _serverData );
        }

        public IBinaryStore CreateBinaryStore( string commandParameterIdentifier )
        {
            return this;
        }
    }
}
