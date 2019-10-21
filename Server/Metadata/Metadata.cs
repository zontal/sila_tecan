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
    /// <summary>
    /// Denotes a helper class for handling metadata
    /// </summary>
    public static class Metadata
    {
        /// <summary>
        /// Gets the list of commands and properties affected by the given interceptor
        /// </summary>
        /// <param name="server">The server</param>
        /// <param name="interceptor">The metadata interceptor</param>
        /// <returns>A task that returns the list of affected identifiers</returns>
        public static Task<List<StringDto>> GetAffectedIdentifiers(ISiLAServer server, IRequestInterceptor interceptor)
        {
            var affected = new List<StringDto>();

            foreach (var provider in server.Features)
            {
                var feature = provider.FeatureDefinition;
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

        /// <summary>
        /// Extracts the metadata with the given identifier from the call context
        /// </summary>
        /// <typeparam name="TMetadata">The type of the metadata</typeparam>
        /// <param name="context">The context in which the call was made</param>
        /// <param name="metadataIdentifier">The global unique identifier of the metadata</param>
        /// <returns>The deserialized metadata</returns>
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
                        return Serializer.Deserialize<TMetadata>(ms);
                    }
                }
            }

            return new TMetadata();
        }
    }
}
