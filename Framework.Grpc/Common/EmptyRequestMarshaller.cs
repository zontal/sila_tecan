using Grpc.Core;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes a class to marshal empty requests
    /// </summary>
    public static class EmptyRequestMarshaller
    {
        /// <summary>
        /// Gets a marshaller instance that handles empty requests
        /// </summary>
        public static readonly Marshaller<EmptyRequest> Instance = new Marshaller<EmptyRequest>( EmptyRequest.Marshaller.Serializer, EmptyRequest.Marshaller.Deserializer );
    }
}
