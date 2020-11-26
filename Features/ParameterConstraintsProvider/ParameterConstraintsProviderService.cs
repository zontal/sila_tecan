using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.ParameterConstraintsProvider
{
    [Export(typeof( IParameterConstraintsProvider ) )]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ParameterConstraintsProviderService : IParameterConstraintsProvider
    {

        private readonly Lazy<ISiLAServer> _server;
        private readonly Lazy<Dictionary<string, KeyValuePair<ConstraintProviderAttribute, IParameterConstraintResolver>>> _constraintsSource;
        private readonly Dictionary<Type, IParameterConstraintResolver> _resolvers;
        private readonly ILoggingChannel _loggingChannel;

        [ImportingConstructor]
        public ParameterConstraintsProviderService( Lazy<ISiLAServer> server, [ImportMany] IEnumerable<IParameterConstraintResolver> resolvers, ILoggingChannelFactory loggingChannelFactory )
        {
            _server = server;
            _constraintsSource = new Lazy<Dictionary<string, KeyValuePair<ConstraintProviderAttribute, IParameterConstraintResolver>>>(InitializeConstraintsSource);
            _resolvers = resolvers.ToDictionary( r => r.ConstraintAttributeType );
            _loggingChannel = loggingChannelFactory.CreateChannel( typeof(ParameterConstraintsProviderService) );
        }

        private Dictionary<string, KeyValuePair<ConstraintProviderAttribute, IParameterConstraintResolver>> InitializeConstraintsSource()
        {
            var source = new Dictionary<string, KeyValuePair<ConstraintProviderAttribute, IParameterConstraintResolver>>();
            foreach( var featureProvider in _server.Value.Features )
            {
                var feature = featureProvider.FeatureDefinition;
                foreach( var command in featureProvider.FeatureDefinition.Items.OfType<FeatureCommand>() )
                {
                    var method = featureProvider.GetCommand( feature.GetFullyQualifiedIdentifier( command ) );

                    foreach( var parameterInfo in method.GetParameters() )
                    {
                        if(parameterInfo.GetCustomAttributes( typeof( ConstraintProviderAttribute ), false ).FirstOrDefault() is ConstraintProviderAttribute attribute)
                        {
                            var parameterIdentifier = parameterInfo.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? parameterInfo.Name;
                            var fullyQualifiedIdentifier = feature.GetFullyQualifiedParameterIdentifier( command, parameterIdentifier );

                            if( _resolvers.TryGetValue( attribute.GetType(), out var resolver ) )
                            {
                                source.Add( fullyQualifiedIdentifier, new KeyValuePair<ConstraintProviderAttribute, IParameterConstraintResolver>(attribute, resolver));
                            }
                            else
                            {
                                _loggingChannel.Log($"Found constraint with type {attribute.GetType().FullName} but no suitable resolver.".ToString, Severity.Warning);   
                            }
                        }
                    }
                }
            }

            return source;
        }

        private string GetConstraintString( ConstraintProviderAttribute attribute, IParameterConstraintResolver resolver )
        {
            try
            {
                var constraint = resolver.ResolveConstraint( attribute );
                if( constraint != null )
                {
                    return DataTypeSerializer.SaveToString( new DataTypeType()
                    {
                        Item = constraint
                    } );
                }
            }
            catch( Exception e )
            {
                _loggingChannel.LogException( "Failed to obtain runtime constraint.".ToString, e );
            }

            return null;
        }

        public ICollection<ParametersConstraint> ParametersConstraints
        {
            get
            {
                var constraints = new List<ParametersConstraint>();
                foreach( var entry in _constraintsSource.Value )
                {
                    var constraint = GetConstraintString(entry.Value.Key, entry.Value.Value);
                    if( constraint != null )
                    {
                        constraints.Add( new ParametersConstraint( entry.Key, constraint ) );
                    }
                }

                return constraints;
            }
        }
    }
}
