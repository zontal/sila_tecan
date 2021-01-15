using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.ServerPooling
{
    internal class CreateBinaryHandler : IResponseHandler<CreateBinaryResponseDto>
    {
        private readonly TaskCompletionSource<CreateBinaryResponseDto> _responseTask;

        public CreateBinaryHandler()
        {
            _responseTask = new TaskCompletionSource<CreateBinaryResponseDto>();
        }

        public void Cancel()
        {
            _responseTask.SetCanceled();
        }

        public bool ProcessMessage( SilaServerMessage serverMessage )
        {
            if (serverMessage.Type == SilaServerMessageType.CreateBinaryResponse)
            {
                _responseTask.SetResult( serverMessage.CreateBinaryResponse );
                return true;
            }
            return false;
        }

        public Task<CreateBinaryResponseDto> ResponseTask => _responseTask.Task;
    }
}
