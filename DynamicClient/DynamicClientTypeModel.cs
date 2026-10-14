using ProtoBuf;
using ProtoBuf.Meta;

#pragma warning disable PBN9001 // the compile-time model API of protobuf-net is marked as experimental

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// The compile-time generated protobuf-net type model for the data transfer objects of the dynamic client
    /// </summary>
    /// <remarks>
    /// This model does not require reflection or dynamic code generation and therefore works in trimmed and Native AOT applications.
    /// Use <see cref="ByteSerializer.Create{T}(TypeModel)"/> to obtain a serializer for one of its types.
    /// </remarks>
    [ProtoModel]
    [ProtoSerializable( typeof( AnyTypeDto ) )]
    [ProtoSerializable( typeof( PropertyResponse<AnyTypeDto> ) )]
    public sealed partial class DynamicClientTypeModel : TypeModel
    {
    }
}