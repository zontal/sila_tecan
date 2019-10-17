using System;
using System.Linq;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Denotes a context of a feature in which requests are made
    /// </summary>
    public class FeatureContext : ClientBase<FeatureContext>
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
        /// Creates a new feature context
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="channel">The underlying channel</param>
        /// <param name="executionManager">The execution manager that handles requests in the context of this feature</param>
        public FeatureContext(Feature feature, Channel channel, IClientExecutionManager executionManager) : base(channel)
        {
            Feature = feature;
            ExecutionManager = executionManager;
            Serializer = new DynamicObjectSerializer(ResolveType);
        }

        /// <summary>
        /// Creates a new feature context
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="callInvoker">A call invoker that invokes gRPC calls</param>
        /// <param name="executionManager">The execution manager that handles requests in the context of this feature</param>
        public FeatureContext(Feature feature, CallInvoker callInvoker, IClientExecutionManager executionManager) : base(callInvoker)
        {
            Feature = feature;
            ExecutionManager = executionManager;
            Serializer = new DynamicObjectSerializer(ResolveType);
        }

        /// <summary>
        /// Creates a new feature context
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="configuration">The client configuration</param>
        /// <param name="serializer">The serializer that should be used</param>
        /// <param name="executionManager">The execution manager that handles requests in the context of this feature</param>
        protected FeatureContext(Feature feature, DynamicObjectSerializer serializer, IClientExecutionManager executionManager,
            ClientBaseConfiguration configuration) : base(configuration)
        {
            Feature = feature;
            Serializer = serializer;
            ExecutionManager = executionManager;
        }

        internal new CallInvoker CallInvoker => base.CallInvoker;

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
            return ErrorHandling.ConvertException(ex, ConvertException);
        }

        /// <summary>
        /// Converts the given exception
        /// </summary>
        /// <param name="error">The SiLA2 defined error message</param>
        /// <returns>An exception with the actual error message</returns>
        public Exception ConvertException(DefinedExecutionErrorDto error)
        {
            return new Exception(
                $"{error.Message} ({error.ErrorIdentifier})");
        }

        /// <summary>
        /// Gets the name of the service that handles this feature
        /// </summary>
        public string ServiceName => Feature.Namespace + "." + Feature.Identifier;

        /// <inheritdoc />
        protected override FeatureContext NewInstance(ClientBaseConfiguration configuration)
        {
            return new FeatureContext(Feature, Serializer, ExecutionManager, configuration);
        }
    }
}
