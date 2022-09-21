using System.Collections.Generic;
using System.Linq;

namespace Tecan.Sila2.Client.ExecutionManagement
{
    internal class InterceptionRepository
    {
        private readonly IEnumerable<IClientRequestInterceptor> _interceptors;
        private readonly IEnumerable<IClientRequestInterceptor> _ubiquitousInterceptors;

        private readonly Dictionary<string, List<IClientRequestInterceptor>> _cachedInterceptors = new Dictionary<string, List<IClientRequestInterceptor>>();

        public InterceptionRepository( IEnumerable<IClientRequestInterceptor> interceptors )
        {
            _interceptors = interceptors;
            _ubiquitousInterceptors = interceptors?.Where( i => i.MetadataIdentifier == null ).ToList() ?? Enumerable.Empty<IClientRequestInterceptor>();
        }

        public void RegisterAffected( string metadataIdentifier, IEnumerable<string> affected )
        {
            var interceptor = _interceptors?.SingleOrDefault( ic => ic.MetadataIdentifier == metadataIdentifier );
            if(interceptor != null)
            {
                foreach(var affectedCommand in affected)
                {
                    if(!_cachedInterceptors.TryGetValue( affectedCommand, out var interceptors ))
                    {
                        interceptors = new List<IClientRequestInterceptor>();
                        _cachedInterceptors.Add( affectedCommand, interceptors );
                    }

                    interceptors.Add( interceptor );
                }
            }
        }

        private IEnumerable<IClientRequestInterceptor> ConcatIfApplicable( IEnumerable<IClientRequestInterceptor> current, string identifier )
        {
            if(_cachedInterceptors.TryGetValue( identifier, out var interceptors ))
            {
                return interceptors.Concat( current );
            }
            return current;
        }

        public IEnumerable<IClientRequestInterceptor> GetInterceptorsForCommand( string commandIdentifier )
        {
            var commandNameSeparator = commandIdentifier.LastIndexOf( '/' );
            var featureLength = commandIdentifier.LastIndexOf( '/', commandNameSeparator - 1 );
            var featureIdentifier = commandIdentifier.Substring( 0, featureLength );
            return ConcatIfApplicable( ConcatIfApplicable( _ubiquitousInterceptors, featureIdentifier ), commandIdentifier );
        }
    }
}
