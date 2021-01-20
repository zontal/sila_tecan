using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.ServerPooling
{
    internal class GetBinaryInfoHandler : IResponseHandler<GetBinaryInfoResponseDto>
    {
        private readonly TaskCompletionSource<GetBinaryInfoResponseDto> _responseTask = new TaskCompletionSource<GetBinaryInfoResponseDto>();
        private readonly string _binaryStorageId;

        public GetBinaryInfoHandler(string binaryStorageId)
        {
            _binaryStorageId = binaryStorageId;
        }

        public void Cancel()
        {
            _responseTask.SetCanceled();
        }

        public bool ProcessMessage( SilaServerMessage serverMessage )
        {
            if(serverMessage.Type == SilaServerMessageType.GetBinaryResponse)
            {
                _responseTask.SetResult( serverMessage.GetBinaryResponse );
                return true;
            }
            else if(serverMessage.Type == SilaServerMessageType.BinaryError)
            {
                _responseTask.SetException( ErrorHandling.ConvertException( serverMessage.BinaryError, _binaryStorageId ) );
            }
            return false;
        }

        public Task<GetBinaryInfoResponseDto> ResponseTask => _responseTask.Task;
    }
}
