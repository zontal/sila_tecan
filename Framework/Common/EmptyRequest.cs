using Grpc.Core;
using ProtoBuf;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2
{
    /// <summary>
    /// The default singleton implementation for empty requests
    /// </summary>
    [ProtoContract]
    public class EmptyRequest
    {
        /// <summary>
        /// The marshaller for empty requests
        /// </summary>
        public static readonly Marshaller<EmptyRequest> Marshaller = new Marshaller<EmptyRequest>(ToByteArray, FromByteArray);

        /// <summary>
        /// The instance
        /// </summary>
        public static readonly EmptyRequest Instance = new EmptyRequest();
        private static readonly byte[] Data = new byte[0];

        private static byte[] ToByteArray(EmptyRequest empty)
        {
            return Data;
        }

        private static EmptyRequest FromByteArray(byte[] data)
        {
            return Instance;
        }
    }
}
