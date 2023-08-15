using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.Client.ExecutionManagement
{
    internal class ExecutionManager : IClientExecutionManagerEx
    {
        private readonly InterceptionRepository _interceptionRepository;
        private readonly ServerData _serverData;
        private readonly ILog _loggingChannel = LogManager.GetLogger<ExecutionManager>();
        private readonly Dictionary<string, List<string>> _affected = new Dictionary<string, List<string>>();

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
                        AddAffected(feature.GetFullyQualifiedIdentifier(metadata), affected );
                    }
                    catch (Exception ex)
                    {
                        _loggingChannel.Error($"Error reading affected commands and properties for metadata {metadata.DisplayName}. Ignoring metadata.", ex);
                    }
                }
            }
        }

        private void AddAffected(string metadataIdentifier, IEnumerable<string> affected)
        {
            if (affected != null)
            {
                foreach (var affectedCommandId in affected)
                {
                    if (!_affected.TryGetValue(affectedCommandId, out var metadataRequired))
                    {
                        metadataRequired = new List<string>();
                        _affected.Add(affectedCommandId, metadataRequired);
                    }
                    metadataRequired.Add(metadataIdentifier);
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
                var intercept = interceptor.Intercept( _serverData, commandIdentifier, this, metadata );
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

        public IEnumerable<string> GetRequiredMetadata(string commandIdentifier)
        {
            _affected.TryGetValue(commandIdentifier, out var metadata);
            var lstSlash = commandIdentifier.LastIndexOf('/');
            if (lstSlash != -1)
            {
                var secondLastSlash = commandIdentifier.LastIndexOf('/', lstSlash - 1);
                if (secondLastSlash != -1)
                {
                    if (_affected.TryGetValue(commandIdentifier.Substring(0, secondLastSlash), out var metadataOfFeature))
                    {
                        return metadata == null ? metadataOfFeature : metadataOfFeature.Concat(metadata);
                    }
                }
            }
            return metadata ?? Enumerable.Empty<string>();
        }
    }
}
