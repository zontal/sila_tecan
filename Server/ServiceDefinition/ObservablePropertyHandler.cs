using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server.ServiceDefinition
{

    internal class ObservablePropertyHandler<T> : ObservablePropertyHandlerBase<T>
    {
        private readonly INotifyPropertyChanged _changeSource;
        private readonly string _clientPropertyName;
        protected readonly Func<T> _implementation;

        public ObservablePropertyHandler(SiLAServer server, string identifier, Func<T> implementation, INotifyPropertyChanged changeSource, string clientPropertyName, Marshaller<T> marshaller) : base(server, identifier, marshaller)
        {
            _changeSource = changeSource;
            _clientPropertyName = clientPropertyName;
            _implementation = implementation;
        }

        protected override Func<Task<int>> CreateSubscriptionHandler(IAsyncStreamWriter<T> responseStream, CancellationToken cancellationToken)
        {
            return () => SendValueAsync(responseStream, cancellationToken);
        }

        private async Task<int> SendValueAsync(IAsyncStreamWriter<T> responseStream, CancellationToken cancellationToken)
        {
            var channel = Channels.CreateIntermediateChannel<T>();

            void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == _clientPropertyName)
                {
                    try
                    {
                        channel.Writer.TryWrite(_implementation());
                    }
                    catch (Exception propertyReadException)
                    {
                        channel.Writer.TryComplete(propertyReadException);
                    }
                }
            }

            var handler = new PropertyChangedEventHandler(HandlePropertyChanged);

            _changeSource.PropertyChanged += handler;
            try
            {
                await responseStream.WriteAsync(_implementation());
                do
                {
                    if (await channel.Reader.WaitToReadAsync(cancellationToken))
                    {
                        if (channel.Reader.TryRead(out var update))
                        {
                            await responseStream.WriteAsync(update);
                        }
                        else if (channel.Reader.Completion.IsCompleted)
                        {
                            return 0;
                        }
                    }
                } while (!cancellationToken.IsCancellationRequested);

                return 0;
            }
            catch (OperationCanceledException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                return 0;
            }
            finally
            {
                _changeSource.PropertyChanged -= handler;
            }
        }
    }
}
