using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.ServerPooling
{
    internal interface IBinaryStoreChannel
    {
        Task UploadChunk( UploadChunkRequestDto uploadChunkRequestDto );

        Task<CreateBinaryResponseDto> CreateBinaryAsync( CreateBinaryRequestDto createBinaryRequestDto, IClientCallInfo callInfo );

        Task<GetChunkResponseDto> GetChunk( GetChunkRequestDto getChunkRequestDto );

        Task<GetBinaryInfoResponseDto> GetBinaryInfoAsync( GetBinaryInfoRequestDto getBinaryInfoRequestDto );

        Task DeleteBinaryDownloadAsync( DeleteBinaryRequestDto deleteBinaryRequestDto, IClientCallInfo callInfo );
    }
}
