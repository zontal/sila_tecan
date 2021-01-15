using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.ServerPooling
{
    internal class UnobservablePropertyHandler<T> : IResponseHandler<T>
    {
        private readonly TaskCompletionSource<T> _responseTask;
        private readonly string _requestUuid;
        private readonly Func<byte[], T> _responseSerializer;

        public UnobservablePropertyHandler( string requestUuid, Func<byte[], T> responseSerializer )
        {
            _responseTask = new TaskCompletionSource<T>();
            _requestUuid = requestUuid;
            _responseSerializer = responseSerializer;
        }

        public bool ProcessMessage( SilaServerMessage serverMessage )
        {
            switch(serverMessage.Type)
            {
                case SilaServerMessageType.PropertyValue:
                    _responseTask.SetResult( _responseSerializer( serverMessage.PropertyValue.Result ) );
                    return true;
                case SilaServerMessageType.PropertyError:
                    _responseTask.SetException( new TemporaryException( serverMessage.PropertyError.SilaError ) );
                    return true;
                default:
                    return false;
            }
        }

        public void Cancel()
        {
            _responseTask.SetCanceled();
        }

        public Task<T> ResponseTask => _responseTask.Task;
    }
}
