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
            ProtobufMarshaller.Create<CreateBinaryRequestDto>( FrameworkTypeModel.Instance ), ProtobufMarshaller.Create<CreateBinaryResponseDto>( FrameworkTypeModel.Instance ));

        /// <summary>
        /// The method to upload chunks
        /// </summary>
        public static readonly Method<UploadChunkRequestDto, UploadChunkResponseDto> UploadChunk = new Method<UploadChunkRequestDto, UploadChunkResponseDto>(MethodType.DuplexStreaming, BinaryUploadServiceName, nameof(UploadChunk),
            ProtobufMarshaller.Create<UploadChunkRequestDto>( FrameworkTypeModel.Instance ), ProtobufMarshaller.Create<UploadChunkResponseDto>( FrameworkTypeModel.Instance ));

        /// <summary>
        /// The method to delete an uploaded binary
        /// </summary>
        public static readonly Method<DeleteBinaryRequestDto, EmptyRequest> DeleteBinaryUpload = new Method<DeleteBinaryRequestDto, EmptyRequest>(MethodType.Unary, BinaryUploadServiceName, "DeleteBinary",
            ProtobufMarshaller.Create<DeleteBinaryRequestDto>( FrameworkTypeModel.Instance ), EmptyRequestMarshaller.Instance );

        /// <summary>
        /// The method to obtain the binary information
        /// </summary>
        public static readonly Method<GetBinaryInfoRequestDto, GetBinaryInfoResponseDto> GetBinaryInfo = new Method<GetBinaryInfoRequestDto, GetBinaryInfoResponseDto>(MethodType.Unary, BinaryDownloadServiceName, nameof(GetBinaryInfo),
            ProtobufMarshaller.Create<GetBinaryInfoRequestDto>( FrameworkTypeModel.Instance ), ProtobufMarshaller.Create<GetBinaryInfoResponseDto>( FrameworkTypeModel.Instance ));

        /// <summary>
        /// The method to get a chunk
        /// </summary>
        public static readonly Method<GetChunkRequestDto, GetChunkResponseDto> GetChunk = new Method<GetChunkRequestDto, GetChunkResponseDto>(MethodType.DuplexStreaming, BinaryDownloadServiceName, nameof(GetChunk),
            ProtobufMarshaller.Create<GetChunkRequestDto>( FrameworkTypeModel.Instance ), ProtobufMarshaller.Create<GetChunkResponseDto>( FrameworkTypeModel.Instance ));

        /// <summary>
        /// The method to delete a downloaded binary
        /// </summary>
        public static readonly Method<DeleteBinaryRequestDto, EmptyRequest> DeleteBinaryDownload = new Method<DeleteBinaryRequestDto, EmptyRequest>(MethodType.Unary, BinaryDownloadServiceName, "DeleteBinary",
            ProtobufMarshaller.Create<DeleteBinaryRequestDto>( FrameworkTypeModel.Instance ), EmptyRequestMarshaller.Instance );
    }
}
