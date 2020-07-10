using System.Collections.Generic;
using ProtoBuf;

namespace Tecan.Sila2.Client
{
    [ProtoContract]
    internal class MetadataAffectedResponse
    {
        [ProtoMember(1)]
        public List<StringDto> Affected { get; } = new List<StringDto>();
    }
}