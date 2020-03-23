using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Client;
using Tecan.Sila2.Interop.Client.ComplexDataTypeTest;

namespace Tecan.Sila2.Interop.Client
{
    internal class ExecutionManager : IClientExecutionManager
    {
        private readonly Channel _serverChannel;

        public ExecutionManager( Channel serverChannel )
        {
            DownloadBinaryStore = new ClientBinaryStore(null, serverChannel);
            _serverChannel = serverChannel;
        }

        public IClientCallInfo CreateCallOptions( string commandIdentifier )
        {
            var metadata = new Metadata();
            if( commandIdentifier.StartsWith( "org.silastandard/test/ComplexDataTypeTest/v1" ) )
            {
                metadata.Add( ClientMetadata.GetHeaderName( "org.silastandard/test/ComplexDataTypeTest/v1/Metadata/User" ),
                    ProtobufMarshaller<UserDto>.ToByteArray( new UserDto( new User( Environment.UserName ), DownloadBinaryStore ) ) );
            }
            return new CallInfo(new CallOptions(metadata));
        }

        public IBinaryStore DownloadBinaryStore
        {
            get;
        }

        public IBinaryStore CreateBinaryStore( string commandParameterIdentifier )
        {
            return new ClientBinaryStore(commandParameterIdentifier, _serverChannel);
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
