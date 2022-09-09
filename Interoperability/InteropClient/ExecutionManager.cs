using System;
using System.Collections.Generic;
using Grpc.Core;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Client;
using Tecan.Sila2.Interop.Client.MetadataProvider;
using Tecan.Sila2.Interop.Client.MetadataProvider.MetadataProviderTest;

namespace Tecan.Sila2.Interop.Client
{
    internal class ExecutionManager : IClientExecutionManager
    {
        private readonly Channel _serverChannel;

        public ExecutionManager( Channel serverChannel )
        {
            DownloadBinaryStore = new ClientBinaryStore( null, serverChannel, this );
            _serverChannel = serverChannel;
        }

        public IClientCallInfo CreateCallOptions( string commandIdentifier )
        {
            var metadata = new Dictionary<string, byte[]>();
            if(commandIdentifier.StartsWith( "org.silastandard/test/MetadataConsumerTest/v1" ))
            {
                metadata.Add( "org.silastandard/test/MetadataProviderTest/v1/Metadata/StringMetadata",
                    ProtobufMarshaller<StringDto>.ToByteArray( new StringDto( StringValue ) ) );
            }
            if(commandIdentifier == "org.silastandard/test/MetadataConsumerTest/v1/Command/UnpackMetadata")
            {
                metadata.Add( "org.silastandard/test/MetadataProviderTest/v1/Metadata/TwoIntegersMetadata",
                    ProtobufMarshaller<TwoIntegersMetadataDto>.ToByteArray( new TwoIntegersMetadataDto( TwoIntegers, DownloadBinaryStore ) ) );
            }
            return new CallInfo( metadata );
        }

        public string StringValue
        {
            get;
            set;
        }

        public TwoIntegersMetadata TwoIntegers
        {
            get;
            set;
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
