using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.DynamicClient
{
    internal static class MetadataHelper
    {
        public static IClientCallInfo TakeInMetadata( IClientCallInfo callInfo, IDictionary<string, byte[]> additionalMetadata )
        {
            if (additionalMetadata == null || additionalMetadata.Count == 0)
            {
                return callInfo;
            }
            return new AnnotatedCallInfo( callInfo, additionalMetadata );
        }

        private class AnnotatedCallInfo : IClientCallInfo
        {
            private readonly IClientCallInfo _inner;

            public IDictionary<string, byte[]> Metadata
            {
                get;
            }

            public AnnotatedCallInfo(IClientCallInfo inner, IDictionary<string, byte[]> additionalMetadata)
            {
                if(inner.Metadata != null)
                {
                    Metadata = new Dictionary<string, byte[]>( inner.Metadata );
                    foreach(var item in additionalMetadata)
                    {
                        Metadata[item.Key] = item.Value;
                    }
                }
                else
                {
                    Metadata = additionalMetadata;
                }
                _inner = inner;
            }

            public void FinishSuccessful()
            {
                _inner.FinishSuccessful();
            }

            public void FinishWithErrors( Exception exception )
            {
                _inner.FinishWithErrors( exception );
            }
        }
    }
}
