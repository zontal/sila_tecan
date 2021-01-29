using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tecan.Sila2.ServerPooling
{
    internal class UnobservableCommandHandler : IResponseHandler<bool>
    {
        private readonly TaskCompletionSource<bool> _responseTask;

        public UnobservableCommandHandler()
        {
            _responseTask = new TaskCompletionSource<bool>();
        }

        public bool ProcessMessage( SilaServerMessage serverMessage )
        {
            switch(serverMessage.Type)
            {
                case SilaServerMessageType.CommandResponse:
                    _responseTask.SetResult( true );
                    return true;
                case SilaServerMessageType.CommandError:
                    _responseTask.SetException( new TemporaryException( serverMessage.CommandError ) );
                    return true;
                default:
                    return false;
            }
        }

        public void Cancel()
        {
            _responseTask.SetCanceled();
        }

        public Task<bool> ResponseTask => _responseTask.Task;
    }

    internal class UnobservableCommandHandler<TResponse> : IResponseHandler<TResponse>
    {
        private readonly TaskCompletionSource<TResponse> _responseTask;
        private readonly Func<byte[], TResponse> _responseSerializer;

        public UnobservableCommandHandler(Func<byte[], TResponse> responseSerializer)
        {
            _responseTask = new TaskCompletionSource<TResponse>();
            _responseSerializer = responseSerializer;
        }

        public bool ProcessMessage( SilaServerMessage serverMessage )
        {
            switch(serverMessage.Type)
            {
                case SilaServerMessageType.CommandResponse:
                    _responseTask.SetResult( _responseSerializer( serverMessage.CommandResponse.Result ) );
                    return true;
                case SilaServerMessageType.CommandError:
                    _responseTask.SetException( new TemporaryException(serverMessage.CommandError) );
                    return true;
                default:
                    return false;
            }
        }

        public void Cancel()
        {
            _responseTask.SetCanceled();
        }

        public Task<TResponse> ResponseTask => _responseTask.Task;
    }
}
