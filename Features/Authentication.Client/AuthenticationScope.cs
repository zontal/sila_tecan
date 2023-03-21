using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tecan.Sila2.Authentication
{
    public class AuthenticationScope : IDisposable
    {
#if NET6_0_OR_GREATER
        private readonly ConditionalWeakTable<ServerData, AuthTokenHandle> _cachedTokens = new ConditionalWeakTable<ServerData, AuthTokenHandle>();
#else
        private readonly ConcurrentDictionary<ServerData, AuthTokenHandle> _cachedTokens = new ConcurrentDictionary<ServerData, AuthTokenHandle>();
#endif
        private readonly AuthenticationScope _parentScope;

        internal AuthTokenHandle GetToken(ServerData serverData, Func<ServerData, AuthTokenHandle> getTokenMethod)
        {
#if NET6_0_OR_GREATER
            return _cachedTokens.GetValue(serverData, s => getTokenMethod(s));
#else
            return _cachedTokens.GetOrAdd(serverData, getTokenMethod);
#endif
        }

        private static readonly AuthenticationScope _defaultScope = new AuthenticationScope();

        [ThreadStatic]
        private static AuthenticationScope _threadScope;

        public static AuthenticationScope Current
        {
            get
            {
                return _threadScope ?? _defaultScope;
            }
        }

        public AuthenticationScope()
        {
            _parentScope = Current;
            if (_parentScope != null)
            {
                _threadScope = this;
            }
        }

        public void Dispose()
        {
            foreach (var token in _cachedTokens)
            {
                token.Value.Logout();
            }
            _threadScope = _parentScope;
        }
    }
}
