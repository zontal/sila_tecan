using System;
using System.Collections.Generic;
using Grpc.Core;

namespace Tecan.Sila2.Client.ExecutionManagement
{
    internal class CallInfo : IClientCallInfo
    {
        private readonly IEnumerable<IRequestInterception> _interceptions;
        public static readonly CallInfo NoMetadata = new CallInfo( new CallOptions(), new IRequestInterception[0] );

        public CallInfo( CallOptions options, IEnumerable<IRequestInterception> interceptions )
        {
            _interceptions = interceptions;
            Options = options;
        }

        public CallOptions Options { get; }

        public void FinishSuccessful()
        {
            foreach(var interception in _interceptions)
            {
                interception.CompleteSuccessfully();
            }
        }

        public void FinishWithErrors( Exception exception )
        {
            foreach(var interception in _interceptions)
            {
                interception.CompleteWithError( exception );
            }
        }
    }
}
