using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tecan.Sila2.ServerPooling
{
    internal class PropertySubscriptionHandler<T> : IResponseHandler<bool>
    {
        private readonly TaskCompletionSource<bool> _responseTask = new TaskCompletionSource<bool>();
        private readonly Func<byte[], T> _responseSerializer;
        private readonly Action<T> _responseAction;
        private readonly ManualResetEventSlim _firstResponseEvent = new ManualResetEventSlim( false );

        public PropertySubscriptionHandler( Func<byte[], T> responseSerializer, Action<T> responseAction )
        {
            _responseSerializer = responseSerializer;
            _responseAction = responseAction;
        }

        public void Complete()
        {
            _responseTask.SetResult( true );
        }

        public void Cancel()
        {
            _responseTask.SetCanceled();
        }

        public void WaitForFirstResponse(CancellationToken cancellationToken)
        {
            _firstResponseEvent.Wait( cancellationToken );
        }

        public bool ProcessMessage( SilaServerMessage serverMessage )
        {
            switch(serverMessage.Type)
            {
                case SilaServerMessageType.ObservablePropertyValue:
                    _responseAction( _responseSerializer( serverMessage.ObservablePropertyValue.Result ) );
                    if (!_firstResponseEvent.IsSet)
                    {
                        lock(_firstResponseEvent)
                        {
                            if (!_firstResponseEvent.IsSet)
                            {
                                _firstResponseEvent.Set();
                            }
                        }
                    }
                    return true;
                case SilaServerMessageType.PropertyError:
                    _responseTask.SetException( new TemporaryException( serverMessage.PropertyError ) );
                    return true;
                default:
                    return false;
            }
        }

        public Task<bool> ResponseTask => _responseTask.Task;
    }
}
