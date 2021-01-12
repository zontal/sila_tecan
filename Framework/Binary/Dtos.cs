using System.ComponentModel;
using ProtoBuf;
// ReSharper disable InconsistentNaming

namespace Tecan.Sila2.Binary
{
    /// <summary>
    /// The data transfer object for a request to create a binary
    /// </summary>
    [ProtoContract]
    public partial class CreateBinaryRequestDto
    {
        /// <summary>
        /// The size of the binary
        /// </summary>
        [ProtoMember(1)]
        public ulong BinarySize { get; set; }

        /// <summary>
        /// The number of chunks to expect
        /// </summary>
        [ProtoMember(2)]
        public uint ChunkCount { get; set; }

        /// <summary>
        /// The command parameter identifier
        /// </summary>
        [ProtoMember(3)]
        public string CommandIdentifier
        {
            get;
            set;
        }

    }

    /// <summary>
    /// The data transfer object for the response from creating a binary
    /// </summary>
    [ProtoContract()]
    public partial class CreateBinaryResponseDto
    {
        /// <summary>
        /// The binary transfer identifier
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue("")]
        public string BinaryTransferUUID { get; set; } = "";

        /// <summary>
        /// The lifetime of the binary
        /// </summary>
        [ProtoMember(2)]
        public TimestampDto LifetimeOfBinary { get; set; }

    }

    /// <summary>
    /// The data transfer object for the request to upload a chunk
    /// </summary>
    [ProtoContract()]
    public partial class UploadChunkRequestDto
    {
        /// <summary>
        /// The binary transfer identifier
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue("")]
        public string BinaryTransferUUID { get; set; } = "";

        /// <summary>
        /// The index of the uploaded chunk
        /// </summary>
        [ProtoMember(2)]
        public uint ChunkIndex { get; set; }

        /// <summary>
        /// The uploaded data
        /// </summary>
        [ProtoMember(3, Name = @"payload")]
        public byte[] Payload { get; set; }

    }

    /// <summary>
    /// The data transfer for the upload chunk response
    /// </summary>
    [ProtoContract()]
    public partial class UploadChunkResponseDto
    {
        /// <summary>
        /// The binary storage identifier
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue("")]
        public string BinaryTransferUUID { get; set; } = "";

        /// <summary>
        /// The index of the chunk
        /// </summary>
        [ProtoMember(2)]
        public uint ChunkIndex { get; set; }

        /// <summary>
        /// The lifetime of the binary
        /// </summary>
        [ProtoMember(3)]
        public TimestampDto LifetimeOfBinary { get; set; }

    }

    /// <summary>
    /// The data transfer object of the request to delete a binary
    /// </summary>
    [ProtoContract()]
    public partial class DeleteBinaryRequestDto
    {
        /// <summary>
        /// The binary storage identifier
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue("")]
        public string BinaryTransferUUID { get; set; } = "";

    }

    /// <summary>
    /// The data transfer object to request information for a binary
    /// </summary>
    [ProtoContract()]
    public partial class GetBinaryInfoRequestDto
    {
        /// <summary>
        /// The binary storage identifier
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue("")]
        public string BinaryTransferUUID { get; set; } = "";

    }

    /// <summary>
    /// The data transfer object for a response of the get binary info command
    /// </summary>
    [ProtoContract()]
    public partial class GetBinaryInfoResponseDto
    {
        /// <summary>
        /// The size of the binary
        /// </summary>
        [ProtoMember(1)]
        public ulong BinarySize { get; set; }

        /// <summary>
        /// The lifetime of the binary
        /// </summary>
        [ProtoMember(2)]
        public TimestampDto LifetimeOfBinary { get; set; }

    }

    /// <summary>
    /// The data transfer object for the request of the get chunk command
    /// </summary>
    [ProtoContract()]
    public partial class GetChunkRequestDto
    {
        /// <summary>
        /// The binary storage identifier
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue("")]
        public string BinaryTransferUUID { get; set; } = "";

        /// <summary>
        /// The offset in the file
        /// </summary>
        [ProtoMember(2, Name = @"offset")]
        public ulong Offset { get; set; }

        /// <summary>
        /// The length
        /// </summary>
        [ProtoMember(3, Name = @"length")]
        public uint Length { get; set; }

    }

    /// <summary>
    /// The data transfer object for the response of the get chunk command
    /// </summary>
    [ProtoContract()]
    public partial class GetChunkResponseDto
    {
        /// <summary>
        /// The binary storage identifier
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue("")]
        public string BinaryTransferUUID { get; set; } = "";

        /// <summary>
        /// The offset in the file
        /// </summary>
        [ProtoMember(2, Name = @"offset")]
        public ulong Offset { get; set; }

        /// <summary>
        /// The payload
        /// </summary>
        [ProtoMember(3, Name = @"payload")]
        public byte[] Payload { get; set; }

        /// <summary>
        /// The lifetime of the binary
        /// </summary>
        [ProtoMember(4)]
        public TimestampDto LifetimeOfBinary { get; set; }

    }

    /// <summary>
    /// Defines an error for binary transfer
    /// </summary>
    [ProtoContract()]
    public partial class BinaryTransferError
    {
        /// <summary>
        /// The error type
        /// </summary>
        [ProtoMember(1)]
        public ErrorType Type { get; set; }

        /// <summary>
        /// The error message
        /// </summary>
        [ProtoMember(2, Name = @"message")]
        [DefaultValue("")]
        public string Message { get; set; } = "";

        /// <summary>
        /// Denotes the possible errors for binary transfer
        /// </summary>
        [ProtoContract()]
        public enum ErrorType
        {
            /// <summary>
            /// Denotes that the chunk index is invalid
            /// </summary>
            [ProtoEnum(Name = @"INVALID_BINARY_CHUNK")]
            InvalidBinaryChunk = 0,
            /// <summary>
            /// Denotes that the binary transfer identifier is invalid
            /// </summary>
            [ProtoEnum(Name = @"INVALID_BINARY_TRANSFER_UUID")]
            InvalidBinaryTransferUuid = 1,
            /// <summary>
            /// Defines that the upload is impossible
            /// </summary>
            [ProtoEnum(Name = @"BINARY_UPLOAD_IMPOSSIBLE")]
            BinaryUploadImpossible = 2,
        }

    }
}
