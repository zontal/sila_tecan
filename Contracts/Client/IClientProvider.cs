using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Denotes an interface to obtain a strongly typed client for the given server
    /// </summary>
    public interface IClientProvider
    {
        /// <summary>
        /// Tries to create a client of the respective type to the given server
        /// </summary>
        /// <typeparam name="TClient">The interface type of the client</typeparam>
        /// <param name="server">The server that should be targeted</param>
        /// <param name="client">The created client or null, if the server does not support this client interface</param>
        /// <exception cref="NotSupportedException">Thrown if the type of client is not supported</exception>
        /// <returns>True, if the client could be created otherwise False</returns>
        bool TryCreateClient<TClient>( ServerData server, out TClient client ) where TClient : class;
    }
}
