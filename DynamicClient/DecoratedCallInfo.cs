using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.DynamicClient
{
    internal class DecoratedCallInfo : IClientCallInfo
    {
        private readonly IClientCallInfo _innerCallInfo;
        private readonly IDictionary<string, byte[]> _additionalMetadata;

        public DecoratedCallInfo( IClientCallInfo innerCallInfo, IDictionary<string, byte[]> additionalMetadata )
        {
            _innerCallInfo = innerCallInfo;
            _additionalMetadata = additionalMetadata;
        }

        public CallOptions Options => MetadataHelper.TakeInMetadata( _innerCallInfo, _additionalMetadata );

        public void FinishSuccessful()
        {
            _innerCallInfo.FinishSuccessful();
        }

        public void FinishWithErrors( Exception exception )
        {
            _innerCallInfo.FinishWithErrors( exception );
        }
    }
}
