using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Tecan.Sila2.Interop.Server.MultiClientTest
{
    [Export( typeof( IMultiClientTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class MultiClientTest : IMultiClientTest, IDisposable
    {
        private object _parallelExceptionLock = new object();

        public IObservableCommand RejectParallelExecution( [Unit( "s", Second = 1 )] double duration )
        {
            return ObservableCommands.Create( update => RejectParallelExecution( TimeSpan.FromSeconds( duration ), update ) );
        }

        private async Task RejectParallelExecution( TimeSpan duration, Action<double, TimeSpan, CommandState> update )
        {
            if(Monitor.TryEnter( _parallelExceptionLock ))
            {
                update( 0, duration, CommandState.Running );
                await Task.Delay( duration );
                Monitor.Exit( _parallelExceptionLock );
            }
            else
            {
                throw new CommandExecutionRejectedException();
            }
        }

        public IObservableCommand RunInParallel( [Unit( "s", Second = 1 )] double duration )
        {
            return ObservableCommands.Create( () => Task.Delay( TimeSpan.FromSeconds( duration ) ) );
        }

        private Channel<(double Duration, TaskCompletionSource<bool> CompletionSource)> _queue = Channel.CreateUnbounded<(double, TaskCompletionSource<bool>)>( new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        } );
        private Thread _queueThread;

        public IObservableCommand RunQueued( [Unit( "s", Second = 1 )] double duration )
        {
            return ObservableCommands.Create( () =>
            {
                var tcs = new TaskCompletionSource<bool>();
                _queue.Writer.WriteAsync( (duration, tcs) );
                return tcs.Task;
            } );
        }

        public void Dispose()
        {
            _queue.Writer.Complete();
        }

        public MultiClientTest()
        {
            _queueThread = new Thread( ExecuteQueuedCommands );
            _queueThread.Start();
        }

        private async void ExecuteQueuedCommands()
        {
            while(await _queue.Reader.WaitToReadAsync())
            {
                if(_queue.Reader.TryRead( out var task ))
                {
                    await Task.Delay( TimeSpan.FromSeconds( task.Duration ) );
                    task.CompletionSource.SetResult( true );
                }
            }
        }
    }
}
