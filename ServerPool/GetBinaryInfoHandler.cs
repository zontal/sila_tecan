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
            return false;
        }

        public Task<GetBinaryInfoResponseDto> ResponseTask => _responseTask.Task;
    }
}
