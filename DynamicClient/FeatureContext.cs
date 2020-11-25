using System;
using System.Linq;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Denotes a context of a feature in which requests are made
    /// </summary>
    public class FeatureContext
    {
        /// <summary>
        /// The feature that this context is about
        /// </summary>
        public Feature Feature { get; }

        /// <summary>
        /// The serializer that is used to serialize requests in the context of this feature
        /// </summary>
        public DynamicObjectSerializer Serializer { get; }

        /// <summary>
        /// The execution manager that handles requests in the context of this feature
        /// </summary>
        public IClientExecutionManager ExecutionManager { get; }

        /// <summary>
        /// Gets the channel to the server
        /// </summary>
        public IClientChannel Channel => ServerData.Channel;

        /// <summary>
        /// The server in the context of which the call is made
        /// </summary>
        public ServerData ServerData
        {
            get;
        }

        /// <summary>
        /// Creates a new feature context
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="serverData">The server in the context of which the call is made</param>
        /// <param name="executionManager">The execution manager that handles requests in the context of this feature</param>
        public FeatureContext(Feature feature, ServerData serverData, IClientExecutionManager executionManager)
        {
            Feature = feature;
            ExecutionManager = executionManager;
            Serializer = new DynamicObjectSerializer(ResolveType);
            ServerData = serverData;
        }

        /// <summary>
        /// Resolves the given type name in the context of this feature
        /// </summary>
        /// <param name="typeName">The name of the requested type</param>
        /// <returns>The description of this type or null, if the type could not be found.</returns>
        public DataTypeType ResolveType(string typeName)
        {
            return Feature.Items.OfType<SiLAElement>().FirstOrDefault(t => t.Identifier == typeName)
                ?.DataType;
        }

        /// <summary>
        /// Converts the given exception
        /// </summary>
        /// <param name="ex">The gRPC exception</param>
        /// <returns>A human-readable exception</returns>
        public Exception ConvertException(Exception ex)
        {
            return Channel.ConvertException(ex, ConvertException);
        }

        /// <summary>
        /// Converts the given exception
        /// </summary>
        /// <param name="errorIdentifier">The SiLA2 defined error identifier</param>
        /// <param name="errorMessage">The actual error message</param>
        /// <returns>An exception with the actual error message</returns>
        public Exception ConvertException(string errorIdentifier, string errorMessage)
        {
            return new DefinedErrorException( errorIdentifier, errorMessage );
        }

        /// <summary>
        /// Gets the name of the service that handles this feature
        /// </summary>
        public string ServiceName => Feature.Namespace + "." + Feature.Identifier;
    }
}
