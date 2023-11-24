using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class ObservablePollingHandler<T> : ObservablePropertyHandlerBase<T>
    {
        protected readonly Func<T> _implementation;

        public ObservablePollingHandler(SiLAServer server, string identifier, Func<T> implementation) : base(server, identifier)
        {
            _implementation = implementation;
        }

        protected override Func<Task<int>> CreateSubscriptionHandler(IAsyncStreamWriter<T> responseStream, CancellationToken cancellationToken)
        {
            return () => SendValueAsync(responseStream, 1000, cancellationToken);
        }

        private async Task<int> SendValueAsync(IAsyncStreamWriter<T> responseStream, int delay, CancellationToken cancellationToken)
        {
            do
            {
                await responseStream.WriteAsync(_implementation());
                await Task.Delay(delay);
            } while (!cancellationToken.IsCancellationRequested);

            return 0;
        }
    }
}
