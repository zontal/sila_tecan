using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.ParameterConstraintsProvider
{
    [Export(typeof( IParameterConstraintsProvider ) )]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ParameterConstraintsProviderService : IParameterConstraintsProvider, INotifyPropertyChanged
    {

        private readonly Lazy<ISiLAServer> _server;
        private readonly Lazy<Dictionary<string, ConstrainedType>> _constraintsSource;
        private readonly Dictionary<Type, IParameterConstraintResolver> _resolvers;
        private readonly ILoggingChannel _loggingChannel;

        public event PropertyChangedEventHandler PropertyChanged;

        [ImportingConstructor]
        public ParameterConstraintsProviderService( Lazy<ISiLAServer> server, [ImportMany] IEnumerable<IParameterConstraintResolver> resolvers, ILoggingChannelFactory loggingChannelFactory )
        {
            _server = server;
            _constraintsSource = new Lazy<Dictionary<string, ConstrainedType>>(InitializeConstraintsSource);
            _resolvers = resolvers.ToDictionary( r => r.ConstraintAttributeType );
            _loggingChannel = loggingChannelFactory.CreateChannel( typeof(ParameterConstraintsProviderService) );
        }

        private Dictionary<string, ConstrainedType> InitializeConstraintsSource()
        {
            var source = new Dictionary<string, ConstrainedType>();
            foreach(var featureProvider in _server.Value.Features)
            {
                var feature = featureProvider.FeatureDefinition;
                foreach(var command in featureProvider.FeatureDefinition.Items.OfType<FeatureCommand>())
                {
                    var method = featureProvider.GetCommand( feature.GetFullyQualifiedIdentifier( command ) );

                    foreach(var parameterInfo in method.GetParameters())
                    {
                        foreach(var attribute in parameterInfo.GetCustomAttributes( false ))
                        {
                            if(_resolvers.TryGetValue( attribute.GetType(), out var resolver ))
                            {
                                var parameterIdentifier = parameterInfo.GetCustomAttribute<SilaIdentifierAttribute>()?.Identifier ?? parameterInfo.Name;
                                var fullyQualifiedIdentifier = feature.GetFullyQualifiedParameterIdentifier( command, parameterIdentifier );

                                try
                                {
                                    var constrained = resolver.ResolveConstraint( attribute as Attribute );
                                    if(constrained != null)
                                    {
                                        source.Add( fullyQualifiedIdentifier, constrained );
                                    }
                                }
                                catch(Exception e)
                                {
                                    _loggingChannel.LogException( "Failed to obtain runtime constraint.".ToString, e );
                                }
                                if(resolver is INotifyParameterConstraintResolver notifyResolver)
                                {
                                    notifyResolver.SubscribeTo( attribute as Attribute, constraint => UpdateConstraint( fullyQualifiedIdentifier, constraint ) );
                                }
                            }
                        }
                    }
                }
            }

            return source;
        }

        private void UpdateConstraint( string fullyQualifiedIdentifier, ConstrainedType constraint )
        {
            if (constraint != null)
            {
                _constraintsSource.Value[fullyQualifiedIdentifier] = constraint;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( ParametersConstraints ) ) );
            }
            else if (_constraintsSource.Value.Remove(fullyQualifiedIdentifier))
            {
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( ParametersConstraints ) ) );
            }
        }

        private string GetConstraintString( ConstrainedType constraint )
        {
            var constraints = constraint?.Constraints;
            if (constraints == null)
            {
                return null;
            }
            return ConstraintSerializer.SaveToString( constraints );
        }

        public ICollection<ParametersConstraint> ParametersConstraints
        {
            get
            {
                var constraints = new List<ParametersConstraint>();
                foreach( var entry in _constraintsSource.Value )
                {
                    var constraint = GetConstraintString(entry.Value);
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
