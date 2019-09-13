using Grpc.Core;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Defines the interface for a component that intercepts requests
    /// </summary>
    public interface IRequestInterceptor
    {
        /// <summary>
        /// Gets the priority of the interceptor
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets a value indicating whether the request interceptor applies to commands
        /// </summary>
        bool AppliesToCommands { get; }

        /// <summary>
        /// Gets a value indicating whether the request interceptor applies to properties
        /// </summary>
        bool AppliesToProperties { get; }


        /// <summary>
        /// Returns whether a given command should be intercepted
        /// </summary>
        /// <param name="feature">The feature that defines the intercepted command</param>
        /// <param name="commandIdentifier">The identifier of the command</param>
        /// <returns>True, if the interceptor should be notified whenever the command with the given identifier is executed, otherwise false</returns>
        bool IsInterceptRequired(Feature feature, string commandIdentifier);

        /// <summary>
        /// Intercepts a call to a given command
        /// </summary>
        /// <param name="commandIdentifier">The identifier of the command</param>
        /// <param name="server">The server that executes the request</param>
        /// <param name="context">The context in which the request was made</param>
        /// <returns>An interception instance</returns>
        IRequestInterception Intercept(string commandIdentifier, ISiLAServer server, ServerCallContext context);
    }
}
