using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tecan.Sila2.Authentication
{
    /// <summary>
    /// A class to organize authentication scopes
    /// </summary>
    public sealed class AuthenticationScope : IDisposable
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

        /// <summary>
        /// Gets the authentication scope of the current thread
        /// </summary>
        public static AuthenticationScope Current
        {
            get
            {
                return _threadScope ?? _defaultScope;
            }
        }

        /// <summary>
        /// Creates a new authentication scope
        /// </summary>
        public AuthenticationScope()
        {
            _parentScope = Current;
            if (_parentScope != null)
            {
#pragma warning disable S3010 // Static fields should not be updated in constructors
                _threadScope = this;
#pragma warning restore S3010 // Static fields should not be updated in constructors
            }
        }

        /// <summary>
        /// Log out and leave the authentication scope
        /// </summary>
        public void Dispose()
        {
            foreach (var token in _cachedTokens)
            {
                token.Value.Logout();
            }
#pragma warning disable S2696 // Instance members should not write to "static" fields
            _threadScope = _parentScope;
#pragma warning restore S2696 // Instance members should not write to "static" fields
        }
    }
}
