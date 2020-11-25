using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2
{
    public static class EmptyRequestMarshaller
    {
        public static readonly Marshaller<EmptyRequest> Instance = new Marshaller<EmptyRequest>( EmptyRequest.Marshaller.Serializer, EmptyRequest.Marshaller.Deserializer );
    }
}
