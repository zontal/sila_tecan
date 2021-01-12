using Grpc.Core;

namespace Tecan.Sila2.Binary
{
    /// <summary>
    /// A static class with constants for binary storage
    /// </summary>
    public static class BinaryStorageConstants
    {
        /// <summary>
        /// The name of the binary upload service
        /// </summary>
        public const string BinaryUploadServiceName = "sila2.org.silastandard.BinaryUpload";

        /// <summary>
        /// The name of the binary download service
        /// </summary>
        public const string BinaryDownloadServiceName = "sila2.org.silastandard.BinaryDownload";

        /// <summary>
        /// The method to create a binary
        /// </summary>
        public static readonly Method<CreateBinaryRequestDto, CreateBinaryResponseDto> CreateBinary = new Method<CreateBinaryRequestDto, CreateBinaryResponseDto>(MethodType.Unary, BinaryUploadServiceName, nameof(CreateBinary),
            ProtobufMarshaller<CreateBinaryRequestDto>.Default, ProtobufMarshaller<CreateBinaryResponseDto>.Default);

        /// <summary>
        /// The method to upload chunks
        /// </summary>
        public static readonly Method<UploadChunkRequestDto, UploadChunkResponseDto> UploadChunk = new Method<UploadChunkRequestDto, UploadChunkResponseDto>(MethodType.DuplexStreaming, BinaryUploadServiceName, nameof(UploadChunk),
            ProtobufMarshaller<UploadChunkRequestDto>.Default, ProtobufMarshaller<UploadChunkResponseDto>.Default);

        /// <summary>
        /// The method to delete an uploaded binary
        /// </summary>
        public static readonly Method<DeleteBinaryRequestDto, EmptyRequest> DeleteBinaryUpload = new Method<DeleteBinaryRequestDto, EmptyRequest>(MethodType.Unary, BinaryUploadServiceName, "DeleteBinary",
            ProtobufMarshaller<DeleteBinaryRequestDto>.Default, EmptyRequestMarshaller.Instance );

        /// <summary>
        /// The method to obtain the binary information
        /// </summary>
        public static readonly Method<GetBinaryInfoRequestDto, GetBinaryInfoResponseDto> GetBinaryInfo = new Method<GetBinaryInfoRequestDto, GetBinaryInfoResponseDto>(MethodType.Unary, BinaryDownloadServiceName, nameof(GetBinaryInfo),
            ProtobufMarshaller<GetBinaryInfoRequestDto>.Default, ProtobufMarshaller<GetBinaryInfoResponseDto>.Default);

        /// <summary>
        /// The method to get a chunk
        /// </summary>
        public static readonly Method<GetChunkRequestDto, GetChunkResponseDto> GetChunk = new Method<GetChunkRequestDto, GetChunkResponseDto>(MethodType.DuplexStreaming, BinaryDownloadServiceName, nameof(GetChunk),
            ProtobufMarshaller<GetChunkRequestDto>.Default, ProtobufMarshaller<GetChunkResponseDto>.Default);

        /// <summary>
        /// The method to delete a downloaded binary
        /// </summary>
        public static readonly Method<DeleteBinaryRequestDto, EmptyRequest> DeleteBinaryDownload = new Method<DeleteBinaryRequestDto, EmptyRequest>(MethodType.Unary, BinaryDownloadServiceName, "DeleteBinary",
            ProtobufMarshaller<DeleteBinaryRequestDto>.Default, EmptyRequestMarshaller.Instance );
    }
}
