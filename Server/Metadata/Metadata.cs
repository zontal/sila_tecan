using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using ProtoBuf;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Server
{
    public static class Metadata
    {
        public static Task<List<StringDto>> GetAffectedIdentifiers(ISiLAServer server, IRequestInterceptor interceptor)
        {
            var affected = new List<StringDto>();

            foreach (var feature in server.Features)
            {
                if (interceptor.AppliesToCommands)
                {
                    foreach (var command in feature.Items.OfType<FeatureCommand>())
                    {
                        var identifier = feature.GetFullyQualifiedIdentifier(command);
                        if (interceptor.IsInterceptRequired(feature, identifier))
                        {
                            affected.Add(new StringDto(identifier));
                        }
                    }
                }
                if (interceptor.AppliesToProperties)
                {
                    foreach (var property in feature.Items.OfType<FeatureProperty>())
                    {
                        var identifier = feature.GetFullyQualifiedIdentifier(property);
                        if (interceptor.IsInterceptRequired(feature, identifier))
                        {
                            affected.Add(new StringDto(identifier));
                        }
                    }
                }
            }

            return Task.FromResult(affected);
        }

        public static TMetadata Extract<TMetadata>(ServerCallContext context, string metadataIdentifier)
            where TMetadata : new()
        {
            var headerIdentifier = ClientMetadata.GetHeaderName(metadataIdentifier);
            foreach (var header in context.RequestHeaders)
            {
                if (string.Equals(header.Key, headerIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    using (var ms = new MemoryStream(header.ValueBytes))
                    {
                        var metadataTransfer = Serializer.Deserialize<TMetadata>(ms);
                    }
                }
            }

            return new TMetadata();
        }
    }
}
