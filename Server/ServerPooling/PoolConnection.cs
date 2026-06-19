using Common.Logging;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Tecan.Sila2.Server;
using Channel = System.Threading.Channels.Channel;

namespace Tecan.Sila2.ServerPooling
{
    internal class PoolConnection : IDisposable, IPoolConnection, IClientStreamWriter<SilaServerMessage>
    {
        private readonly ServerPoolDispatcher _dispatcher;
        private readonly AsyncDuplexStreamingCall<SilaServerMessage, SilaClientMessage> _call;
        private readonly Channel<SilaServerMessage> _channel;
        private readonly ILog _log = LogManager.GetLogger<PoolConnection>();

        public WriteOptions WriteOptions { get => _call.RequestStream.WriteOptions; set => _call.RequestStream.WriteOptions = value; }

        public string Host { get; }

        public bool IsAlive { get; private set; }

        public PoolConnection( ServerPoolDispatcher dispatcher, AsyncDuplexStreamingCall<SilaServerMessage, SilaClientMessage> call, string host )
        {
            Host = host;
            IsAlive = false;

            _dispatcher = dispatcher;
            _call = call;
            _channel = Channel.CreateUnbounded<SilaServerMessage>( new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = false
            } );
        }

        public void Start()
        {
            StartReading();
            StartWriting();
            IsAlive = true;
        }

        private async void StartReading()
        {
            try
            {
                while(await _call.ResponseStream.MoveNext())
                {
                    _dispatcher.HandleClientRequest( _call.ResponseStream.Current, this );
                }
            }
            catch (Exception exception)
            {
                _log.Error( "Error reading message from client", exception );
                IsAlive = false;
            }
            await _call.RequestStream.CompleteAsync();
        }

        private async void StartWriting()
        {
            try
            {
                while(await _channel.Reader.WaitToReadAsync())
                {
                    _channel.Reader.TryRead( out var message );
                    await _call.RequestStream.WriteAsync( message );
                }
            }
            catch (Exception exception)
            {
                _log.Error( "Error sending message to client, aborting connection", exception );
                IsAlive = false;
                await _call.RequestStream.CompleteAsync();
            }
        }

        public void Dispose()
        {
            _call.RequestStream.CompleteAsync().Wait();
        }

        public Task CompleteAsync()
        {
            _channel.Writer.Complete();
            return _call.RequestStream.CompleteAsync();
        }

        public Task WriteAsync( SilaServerMessage message )
        {
            return _channel.Writer.WriteAsync( message ).AsTask();
        }

        public void Restart()
        {
            if (!IsAlive)
            {
                Start();
            }
        }
    }
}
