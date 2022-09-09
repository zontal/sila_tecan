using Common.Logging;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tecan.Sila2.Server
{
    internal class RequestPipeline
    {
        private readonly IEnumerable<IRequestInterceptor> _interceptors;
        private readonly Dictionary<string, List<IRequestInterceptor>> _cachedInterceptorList = new Dictionary<string, List<IRequestInterceptor>>();
        private readonly SiLAServer _server;
        private readonly ILog _logger;
        private readonly MetadataConfigurationSet _metadataConfiguration;
        private readonly IConfigurationStore _configurationStore;
        private readonly string _configurationName;

        public RequestPipeline( SiLAServer server, IEnumerable<IRequestInterceptor> interceptors, IConfigurationStore configurationStore, string configurationName )
        {
            _interceptors = interceptors;
            _server = server;
            _configurationStore = configurationStore;
            _configurationName = configurationName;
            _logger = LogManager.GetLogger( typeof( RequestPipeline ) );
            _metadataConfiguration = _configurationStore.Read<MetadataConfigurationSet>( configurationName )
                ?? new MetadataConfigurationSet();
        }

        private IEnumerable<IRequestInterceptor> GetInterceptors( string commandIdentifier )
        {
            if(!_cachedInterceptorList.TryGetValue( commandIdentifier, out var interceptors ))
            {
                var feature = _server.Features.FirstOrDefault( f => commandIdentifier.StartsWith( f.FeatureDefinition.FullyQualifiedIdentifier ) )?.FeatureDefinition;
                interceptors = _interceptors
                    .Where( i => i.IsInterceptRequired( feature, commandIdentifier )
                        && (i.MetadataIdentifier == null || GetAffectedOfMetadata( i ).Any( commandIdentifier.StartsWith )) )
                    .OrderByDescending( r => r.Priority )
                    .ToList();

                _cachedInterceptorList.Add( commandIdentifier, interceptors );
            }

            return interceptors;
        }

        public void ReportSuccess( Stack<IRequestInterception> interceptions )
        {
            if(interceptions != null)
            {
                foreach(var interceptor in interceptions)
                {
                    try
                    {
                        interceptor.ReportSuccess();
                    }
                    catch(Exception ex)
                    {
                        _logger.Warn( $"Report success to interception {interceptor} resulted in an unexpected exception.", ex );
                    }
                }
            }
        }

        public void ReportError( Stack<IRequestInterception> interceptions, Exception exception )
        {
            if(interceptions != null)
            {
                foreach(var interceptor in interceptions)
                {
                    try
                    {
                        interceptor.ReportException( exception );
                    }
                    catch(Exception ex)
                    {
                        _logger.Warn( $"Report error to interception {interceptor} resulted in an unexpected exception.", ex );
                    }
                }
            }
        }


        /// <summary>
        /// Requests interceptors for a call with the given command identifier
        /// </summary>
        /// <param name="commandIdentifier">Unique identifier of a command or property</param>
        /// <param name="metadataRepository">A repository with metadata information</param>
        /// <returns>A stack of request interceptions</returns>
        public Stack<IRequestInterception> RequestInterceptions( string commandIdentifier, IMetadataRepository metadataRepository )
        {
            Stack<IRequestInterception> interceptions = null;
            try
            {
                foreach(var interceptor in GetInterceptors( commandIdentifier ))
                {
                    var interception = interceptor.Intercept( commandIdentifier, _server, metadataRepository );
                    if(interception != null)
                    {
                        if(interceptions == null) interceptions = new Stack<IRequestInterception>();
                        interceptions.Push( interception );
                    }
                }
            }
            catch(MissingMetadataException missingMetadata)
            {
                _logger.Error( $"Request rejected because required metadata {missingMetadata.MetadataIdentifier} was missing." );
                throw _server.ErrorHandling.CreateMissingMetadataException( missingMetadata.MetadataIdentifier );
            }
            catch(Exception exception)
            {
                if(interceptions != null)
                {
                    while(interceptions.Count > 0)
                    {
                        interceptions.Pop()?.ReportException( exception );
                    }
                }

                if(exception is OperationCanceledException)
                {
                    throw _server.ErrorHandling.CreateCancellationError( exception.Message );
                }
                else if(exception is CommandExecutionRejectedException)
                {
                    throw ServerErrorHandling.CreateFrameworkError( FrameworkErrorType.CommandExecutionNotAccepted );
                }
                else if(!(exception is RpcException))
                {
                    throw _server.ErrorHandling.CreateUndefinedExecutionError( exception );
                }

                throw;
            }

            return interceptions;
        }

        private string[] CalculateAffected( IRequestInterceptor requestInterceptor )
        {
            var affected = new List<string>();

            foreach(var provider in _server.Features)
            {
                var feature = provider.FeatureDefinition;
                if(requestInterceptor.AppliesToCommands)
                {
                    foreach(var command in feature.Items.OfType<FeatureCommand>())
                    {
                        var identifier = feature.GetFullyQualifiedIdentifier( command );
                        if(requestInterceptor.IsInterceptRequired( feature, identifier ))
                        {
                            affected.Add( identifier );
                        }
                    }
                }
                if(requestInterceptor.AppliesToProperties)
                {
                    foreach(var property in feature.Items.OfType<FeatureProperty>())
                    {
                        var identifier = feature.GetFullyQualifiedIdentifier( property );
                        if(requestInterceptor.IsInterceptRequired( feature, identifier ))
                        {
                            affected.Add( identifier );
                        }
                    }
                }
            }

            return affected.ToArray();
        }

        /// <summary>
        /// Gets the affected parts of the given metadata
        /// </summary>
        /// <param name="requestInterceptor">The request interceptor for which the affected metadata should be calculated</param>
        /// <returns>An array with elements affected by this metadata</returns>
        public string[] GetAffectedOfMetadata( IRequestInterceptor requestInterceptor )
        {
            var metadataIdentifier = requestInterceptor.MetadataIdentifier;

            var affected = _metadataConfiguration.Configurations
                ?.FirstOrDefault( c => c.Identifier == metadataIdentifier )
                ?.Affected;

            if(affected != null)
            {
                return affected;
            }

            affected = CalculateAffected( requestInterceptor );
            TryStoreAffectedOfMetadata( metadataIdentifier, affected );
            return affected;
        }

        private void TryStoreAffectedOfMetadata( string metadataIdentifier, string[] affected )
        {
            try
            {
                var config = _metadataConfiguration.Configurations?.FirstOrDefault( c => c.Identifier == metadataIdentifier );
                if(config != null)
                {
                    config.Affected = affected;
                }
                else
                {
                    _metadataConfiguration.Configurations.Add( new MetadataConfiguration
                    {
                        Identifier = metadataIdentifier,
                        Affected = affected
                    } );
                }
                _configurationStore.Write( _configurationName, _metadataConfiguration );
            }
            catch(Exception ex)
            {
                _logger.Warn( $"Could not store affected metadata for {metadataIdentifier}", ex );
            }
        }

    }
}
