using System.Collections.Generic;
using ProtoBuf;
using ProtoBuf.Meta;
using Tecan.Sila2.Binary;
using Tecan.Sila2.ServerPooling;

#pragma warning disable PBN9001 // the compile-time model API of protobuf-net is marked as experimental

namespace Tecan.Sila2
{
    /// <summary>
    /// The compile-time generated protobuf-net type model for the data transfer objects of the SiLA2 framework
    /// </summary>
    /// <remarks>
    /// In contrast to the reflection-based runtime model of protobuf-net, this model does not require reflection
    /// or dynamic code generation and therefore works in trimmed and Native AOT applications.
    /// Use <see cref="ByteSerializer.Create{T}(TypeModel)"/> to obtain a serializer for one of its types.
    /// </remarks>
    [ProtoModel]
    // general framework contracts
    [ProtoSerializable( typeof( EmptyRequest ) )]
    [ProtoSerializable( typeof( EmptyMessage ) )]
    [ProtoSerializable( typeof( CommandConfirmation ) )]
    [ProtoSerializable( typeof( CommandExecutionUuid ) )]
    [ProtoSerializable( typeof( ExecutionState ) )]
    [ProtoSerializable( typeof( SiLAErrorDto ) )]
    [ProtoSerializable( typeof( DefinedExecutionErrorDto ) )]
    [ProtoSerializable( typeof( UndefinedExecutionErrorDto ) )]
    [ProtoSerializable( typeof( ValidationErrorDto ) )]
    [ProtoSerializable( typeof( FrameworkErrorDto ) )]
    // basic types
    [ProtoSerializable( typeof( StringDto ) )]
    [ProtoSerializable( typeof( IntegerDto ) )]
    [ProtoSerializable( typeof( RealDto ) )]
    [ProtoSerializable( typeof( BooleanDto ) )]
    [ProtoSerializable( typeof( BinaryDto ) )]
    [ProtoSerializable( typeof( DateDto ) )]
    [ProtoSerializable( typeof( TimeDto ) )]
    [ProtoSerializable( typeof( TimestampDto ) )]
    [ProtoSerializable( typeof( TimezoneDto ) )]
    [ProtoSerializable( typeof( DurationDto ) )]
    [ProtoSerializable( typeof( GuidDto ) )]
    // closed generic contracts used by the framework
    [ProtoSerializable( typeof( PropertyResponse<StringDto> ) )]
    [ProtoSerializable( typeof( PropertyResponse<List<StringDto>> ) )]
    // binary transfer
    [ProtoSerializable( typeof( CreateBinaryRequestDto ) )]
    [ProtoSerializable( typeof( CreateBinaryResponseDto ) )]
    [ProtoSerializable( typeof( UploadChunkRequestDto ) )]
    [ProtoSerializable( typeof( UploadChunkResponseDto ) )]
    [ProtoSerializable( typeof( DeleteBinaryRequestDto ) )]
    [ProtoSerializable( typeof( GetBinaryInfoRequestDto ) )]
    [ProtoSerializable( typeof( GetBinaryInfoResponseDto ) )]
    [ProtoSerializable( typeof( GetChunkRequestDto ) )]
    [ProtoSerializable( typeof( GetChunkResponseDto ) )]
    [ProtoSerializable( typeof( BinaryTransferError ) )]
    // server-initiated connections (server pooling)
    [ProtoSerializable( typeof( SilaServerMessage ) )]
    [ProtoSerializable( typeof( SilaClientMessage ) )]
    [ProtoSerializable( typeof( CommandExecution ) )]
    [ProtoSerializable( typeof( CommandParameter ) )]
    [ProtoSerializable( typeof( ServerPooling.Metadata ) )]
    [ProtoSerializable( typeof( CommandInitiation ) )]
    [ProtoSerializable( typeof( CommandExecutionReference ) )]
    [ProtoSerializable( typeof( CommandResponse ) )]
    [ProtoSerializable( typeof( ObservableCommandConfirmation ) )]
    [ProtoSerializable( typeof( ObservableCommandExecutionInfo ) )]
    [ProtoSerializable( typeof( ObservableCommandResponse ) )]
    [ProtoSerializable( typeof( GetFCPAffectedByMetadataRequest ) )]
    [ProtoSerializable( typeof( GetFCPAffectedByMetadataResponse ) )]
    [ProtoSerializable( typeof( PropertyRead ) )]
    [ProtoSerializable( typeof( CreateBinaryUploadRequest ) )]
    [ProtoSerializable( typeof( PropertyValue ) )]
    public sealed partial class FrameworkTypeModel : TypeModel
    {
    }
}
