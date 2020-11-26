using System;
using System.Collections.Generic;

namespace Tecan.Sila2.Client.ExecutionManagement
{
    internal class CallInfo : IClientCallInfo
    {
        private readonly IEnumerable<IClientRequestInterception> _interceptions;

        public CallInfo( IDictionary<string, byte[]> metadata, IEnumerable<IClientRequestInterception> interceptions )
        {
            _interceptions = interceptions;
            Metadata = metadata;
        }

        public IDictionary<string, byte[]> Metadata { get; }

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
