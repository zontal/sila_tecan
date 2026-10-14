using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.ServerPooling
{
    /// <summary>
    /// Denotes the gRPC methods for server pooling
    /// </summary>
    public static class PoolingConstants
    {
        /// <summary>
        /// Denotes the gRPC method used for server pooling
        /// </summary>
        public static readonly Method<SilaServerMessage, SilaClientMessage> ConnectSilaServerMethod = new Method<SilaServerMessage, SilaClientMessage>(
            MethodType.DuplexStreaming,
            "sila2.org.silastandard.CloudClientEndpoint",
            "ConnectSILAServer",
            ProtobufMarshaller.Create<SilaServerMessage>( FrameworkTypeModel.Instance ),
            ProtobufMarshaller.Create<SilaClientMessage>( FrameworkTypeModel.Instance ) ); 
    }
}
