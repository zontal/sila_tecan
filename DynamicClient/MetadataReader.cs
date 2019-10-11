using System.Collections.Generic;
using System.Linq;
using Grpc.Core;
using ProtoBuf;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Denotes a class that reads metadata
    /// </summary>
    public class MetadataReader : ClientBase<MetadataReader>
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        public MetadataReader()
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="configuration">The client configuration</param>
        protected MetadataReader(ClientBaseConfiguration configuration) : base(configuration)
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="channel">The underlying gRPC channel</param>
        public MetadataReader(Channel channel) : base(channel)
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="callInvoker">The underlying gRPC call invoker</param>
        public MetadataReader(CallInvoker callInvoker) : base(callInvoker)
        {
        }

        /// <summary>
        /// Gets identifiers of features, commands and properties that are affected by the given metadata
        /// </summary>
        /// <param name="feature">The feature that contains the metadata</param>
        /// <param name="metadata">The metadata model</param>
        /// <returns>A collection of fully qualified identifiers</returns>
        public IEnumerable<string> GetAffected(Feature feature, FeatureMetadata metadata)
        {
            var serviceName = feature.Namespace + "." + feature.Identifier;
            var method = new Method<EmptyRequest, MetadataAffectedResponse>(MethodType.Unary, serviceName,
                $"Get_FCPAffectedByMetadata_{metadata.Identifier}", EmptyRequest.Marshaller, ProtobufMarshaller<MetadataAffectedResponse>.Default);

            var result = CallInvoker.BlockingUnaryCall(method, null, new CallOptions(), EmptyRequest.Instance);
            return result.Affected.Select(dto => dto.Value);
        }


        /// <inheritdoc />
        protected override MetadataReader NewInstance(ClientBaseConfiguration configuration)
        {
            return new MetadataReader(configuration);
        }
    }

    [ProtoContract]
    internal class MetadataAffectedResponse
    {
        [ProtoMember(1)]
        public List<StringDto> Affected { get; } = new List<StringDto>();
    }
}
