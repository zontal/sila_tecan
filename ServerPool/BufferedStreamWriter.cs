using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Channel = System.Threading.Channels.Channel;

namespace Tecan.Sila2.ServerPooling
{
    internal class BufferedStreamWriter<T> : IServerStreamWriter<T>
    {
        private readonly IServerStreamWriter<T> _inner;
        private readonly Channel<T> _channel;

        public BufferedStreamWriter(IServerStreamWriter<T> inner)
        {
            _inner = inner;
            _channel = Channel.CreateUnbounded<T>( new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = false
            } );
        }

        public async Task StartWriting(CancellationToken cancellationToken)
        {
            cancellationToken.Register( Complete );
            while(await _channel.Reader.WaitToReadAsync( cancellationToken ))
            {
                _channel.Reader.TryRead( out var message );
                await _inner.WriteAsync( message );
            }
        }

        public void Complete()
        {
            _channel.Writer.Complete();
        }

        public WriteOptions WriteOptions { get => _inner.WriteOptions; set => _inner.WriteOptions = value; }

        public Task WriteAsync( T message )
        {
            return _channel.Writer.WriteAsync( message ).AsTask();
        }
    }
}
