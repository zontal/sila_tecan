using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.Client.ExecutionManagement
{
    internal class ExecutionManager : IClientExecutionManager
    {
        private readonly InterceptionRepository _interceptionRepository;
        private readonly ServerData _serverData;
        private readonly ILog _loggingChannel = LogManager.GetLogger<ExecutionManager>();

        public IBinaryStore DownloadBinaryStore { get; }

        public ExecutionManager( ServerData serverData, IEnumerable<IClientRequestInterceptor> interceptors )
        {
            _interceptionRepository = new InterceptionRepository( interceptors );
            _serverData = serverData;

            FindAllMetadata();

            DownloadBinaryStore = serverData.Channel.CreateBinaryStore( null, this );
        }

        private void FindAllMetadata()
        {
            foreach(var feature in _serverData.Features)
            {
                var serviceName = feature.Namespace + "." + feature.Identifier;
                foreach(var metadata in feature.Items.OfType<FeatureMetadata>())
                {
                    try
                    {
                        var affected = _serverData.Channel.GetAffected(serviceName, metadata.Identifier, feature.GetFullyQualifiedIdentifier(metadata));
                        _interceptionRepository.RegisterAffected(feature.GetFullyQualifiedIdentifier(metadata), affected );
                    }
                    catch (Exception ex)
                    {
                        _loggingChannel.Error($"Error reading affected commands and properties for metadata {metadata.DisplayName}. Ignoring metadata.", ex);
                    }
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
            return _serverData.Channel.CreateBinaryStore( commandParameterIdentifier, this );
        }
    }
}
