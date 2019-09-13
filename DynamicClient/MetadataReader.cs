using System.Collections.Generic;
using System.Linq;
using Grpc.Core;
using ProtoBuf;

namespace Tecan.Sila2.DynamicClient
{
    public class MetadataReader : ClientBase<MetadataReader>
    {


        public MetadataReader()
        {
        }

        protected MetadataReader(ClientBaseConfiguration configuration) : base(configuration)
        {
        }

        public MetadataReader(Channel channel) : base(channel)
        {
        }

        public MetadataReader(CallInvoker callInvoker) : base(callInvoker)
        {
        }

        public IEnumerable<string> GetAffected(Feature feature, FeatureMetadata metadata)
        {
            var serviceName = feature.Namespace + "." + feature.Identifier;
            var method = new Method<EmptyRequest, MetadataAffectedResponse>(MethodType.Unary, serviceName,
                $"Get_FCPAffectedByMetadata_{metadata.Identifier}", EmptyRequest.Marshaller, ProtobufMarshaller<MetadataAffectedResponse>.Default);

            var result = CallInvoker.BlockingUnaryCall(method, null, new CallOptions(), EmptyRequest.Instance);
            return result.Affected.Select(dto => dto.Value);
        }

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
