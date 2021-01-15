using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.ServerPooling
{
    internal class MetadataHandler : IResponseHandler<IEnumerable<string>>
    {
        private readonly TaskCompletionSource<IEnumerable<string>> _responseTask;

        public MetadataHandler()
        {
            _responseTask = new TaskCompletionSource<IEnumerable<string>>();
        }
        public void Cancel()
        {
            _responseTask.SetCanceled();
        }

        public bool ProcessMessage( SilaServerMessage serverMessage )
        {
            if (serverMessage.Type == SilaServerMessageType.MetadataResponse)
            {
                _responseTask.SetResult( serverMessage.MetadataResponse.AffectedCalls );
                return true;
            }
            return false;
        }

        public Task<IEnumerable<string>> ResponseTask => _responseTask.Task;
    }
}
