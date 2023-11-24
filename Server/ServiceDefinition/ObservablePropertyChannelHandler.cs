using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class ObservablePropertyChannelHandler<T> : ObservablePropertyHandlerBase<T>
    {
        private readonly Func<ChannelReader<T>> _channelFactory;

        public ObservablePropertyChannelHandler(SiLAServer server, string identifier, Func<ChannelReader<T>> channelFactory) : base(server, identifier)
        {
            _channelFactory = channelFactory;
        }

        protected override Func<Task<int>> CreateSubscriptionHandler(IAsyncStreamWriter<T> responseStream, CancellationToken cancellationToken)
        {
            return () => SendValueAsync(responseStream, cancellationToken);
        }

        private async Task<int> SendValueAsync(IAsyncStreamWriter<T> responseStream, CancellationToken cancellationToken)
        {
            var channel = _channelFactory();
            while (!cancellationToken.IsCancellationRequested)
            {
                await channel.WaitToReadAsync(cancellationToken);
                if (channel.TryRead(out var value))
                {
#if NETSTANDARD2_0
                    await responseStream.WriteAsync(value);
#else
                    await responseStream.WriteAsync(value, cancellationToken);               
#endif
                }
            }
            return 0;
        }
    }
}
