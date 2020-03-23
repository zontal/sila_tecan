using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.IntegrationTests
{
    internal class TestAuthLockingExecutionManager : IClientExecutionManager
    {
        private readonly string _lockingIdentifier;
        private readonly string _accessToken;

        public TestAuthLockingExecutionManager( string lockingIdentifier, string accessToken, Channel serverChannel )
        {
            _lockingIdentifier = lockingIdentifier;
            _accessToken = accessToken;
            DownloadBinaryStore = new ClientBinaryStore(null, serverChannel);
        }

        public IClientCallInfo CreateCallOptions( string commandIdentifier )
        {
            var metadata = new Metadata();
            AddIfNotNull( _lockingIdentifier, "org.silastandard/core/LockController/v1/Metadata/LockIdentifier", metadata );
            AddIfNotNull(_accessToken, "org.silastandard/core/AuthorizationService/v1/Metadata/AccessToken", metadata);
            return new CallInfo(new CallOptions(headers: metadata));
        }

        private void AddIfNotNull( string content, string identifier, Metadata metadata )
        {
            if( content != null )
            {
                metadata.Add(ClientMetadata.GetHeaderName(identifier), ProtobufMarshaller<StringDto>.Default.Serializer( content ));
            }
        }

        public IBinaryStore DownloadBinaryStore
        {
            get;
        }

        public IBinaryStore CreateBinaryStore( string commandParameterIdentifier )
        {
            return DownloadBinaryStore;
        }

        private class CallInfo : IClientCallInfo
        {
            public CallInfo( CallOptions options )
            {
                Options = options;
            }

            public CallOptions Options
            {
                get;
            }

            public void FinishSuccessful()
            {
            }

            public void FinishWithErrors( Exception exception )
            {
            }
        }
    }
}
