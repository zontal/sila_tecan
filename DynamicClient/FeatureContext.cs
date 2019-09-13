using System;
using System.Linq;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.DynamicClient
{
    public class FeatureContext : ClientBase<FeatureContext>
    {
        public Feature Feature { get; }

        public DynamicObjectSerializer Serializer { get; }

        public IClientExecutionManager ExecutionManager { get; }

        public FeatureContext(Feature feature, Channel channel, IClientExecutionManager executionManager) : base(channel)
        {
            Feature = feature;
            ExecutionManager = executionManager;
            Serializer = new DynamicObjectSerializer(ResolveType);
        }

        public FeatureContext(Feature feature, CallInvoker callInvoker, IClientExecutionManager executionManager) : base(callInvoker)
        {
            Feature = feature;
            ExecutionManager = executionManager;
            Serializer = new DynamicObjectSerializer(ResolveType);
        }

        protected FeatureContext(Feature feature, DynamicObjectSerializer serializer, IClientExecutionManager executionManager,
            ClientBaseConfiguration configuration) : base(configuration)
        {
            Feature = feature;
            Serializer = serializer;
            ExecutionManager = executionManager;
        }

        internal new CallInvoker CallInvoker => base.CallInvoker;

        public DataTypeType ResolveType(string typeName)
        {
            return Feature.Items.OfType<SiLAElement>().FirstOrDefault(t => t.Identifier == typeName)
                ?.DataType;
        }

        public Exception ConvertException(Exception ex)
        {
            return ErrorHandling.ConvertException(ex, ConvertException);
        }

        public Exception ConvertException(DefinedExecutionErrorDto error)
        {
            return new Exception(
                $"{error.Message} ({error.ErrorIdentifier})");
        }

        public string ServiceName => Feature.Namespace + "." + Feature.Identifier;
        protected override FeatureContext NewInstance(ClientBaseConfiguration configuration)
        {
            return new FeatureContext(Feature, Serializer, ExecutionManager, configuration);
        }
    }
}
