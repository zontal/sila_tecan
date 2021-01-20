using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.ServerPooling
{
    internal class GetChunkHandler : IResponseHandler<GetChunkResponseDto>
    {
        private readonly TaskCompletionSource<GetChunkResponseDto> _responseTask = new TaskCompletionSource<GetChunkResponseDto>();
        private readonly string _binaryStorageId;

        public GetChunkHandler(string binaryStorageId)
        {
            _binaryStorageId = binaryStorageId;
        }

        public void Cancel()
        {
            _responseTask.SetCanceled();
        }

        public bool ProcessMessage( SilaServerMessage serverMessage )
        {
            if (serverMessage.Type == SilaServerMessageType.GetChunkResponse)
            {
                _responseTask.SetResult( serverMessage.GetChunkResponse );
                return true;
            }
            else if(serverMessage.Type == SilaServerMessageType.BinaryError)
            {
                _responseTask.SetException( ErrorHandling.ConvertException( serverMessage.BinaryError, _binaryStorageId ) );
            }
            return false;
        }

        public Task<GetChunkResponseDto> ResponseTask => _responseTask.Task;
    }
}
