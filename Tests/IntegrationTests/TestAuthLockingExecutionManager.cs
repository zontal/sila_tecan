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
        private readonly Channel _serverChannel;

        public TestAuthLockingExecutionManager( string lockingIdentifier, string accessToken, Channel serverChannel )
        {
            _lockingIdentifier = lockingIdentifier;
            _accessToken = accessToken;
            _serverChannel = serverChannel;
            DownloadBinaryStore = new ClientBinaryStore(null, serverChannel, this);
        }

        public IClientCallInfo CreateCallOptions( string commandIdentifier )
        {
            var metadata = new Dictionary<string, byte[]>();
            AddIfNotNull( _lockingIdentifier, "org.silastandard/core/LockController/v1/Metadata/LockIdentifier", metadata );
            AddIfNotNull(_accessToken, "org.silastandard/core/AuthorizationService/v1/Metadata/AccessToken", metadata);
            return new CallInfo(metadata);
        }

        private void AddIfNotNull( string content, string identifier, IDictionary<string, byte[]> metadata )
        {
            if( content != null )
            {
                metadata.Add(identifier, ProtobufMarshaller<StringDto>.Default.Serializer( content ));
            }
        }

        public IBinaryStore DownloadBinaryStore
        {
            get;
        }

        public IBinaryStore CreateBinaryStore( string commandParameterIdentifier )
        {
            return new ClientBinaryStore( commandParameterIdentifier, _serverChannel, this );
        }

        private class CallInfo : IClientCallInfo
        {
            public CallInfo( IDictionary<string, byte[]> metadata )
            {
                Metadata = metadata;
            }

            public IDictionary<string, byte[]> Metadata
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
