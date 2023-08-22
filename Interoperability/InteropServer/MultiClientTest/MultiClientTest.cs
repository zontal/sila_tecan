using ProtoBuf.WellKnownTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
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

        public IObservableCommand RejectParallelExecution( [Unit( "s", Second = 1 )] double duration )
        {
            return new RejectParallelExecutionCommand( TimeSpan.FromSeconds( duration / 2.0 ));
        }

        private class RejectParallelExecutionCommand : ObservableCommand
        {
            private static bool _isAlreadyRunning;
            private TimeSpan _duration;

            public RejectParallelExecutionCommand( TimeSpan duration )
            {
                _duration = duration;
            }

            public override void Start()
            {
                Debug.WriteLine("Requested: " + DateTime.Now);
                // in a production scenario, it would probably be better to use Interlocked.CompareExchange for this
                if (!_isAlreadyRunning)
                {
                    _isAlreadyRunning = true;
                    base.Start();
                }
                else
                {
                    // we need to fail already when creating the task, not inside the task
                    // otherwise the command is created successfully and "only" the result is an error
                    throw new CommandExecutionRejectedException();
                }
            }

            public override async Task Run()
            {
                PushStateUpdate(0, _duration, CommandState.Running);
                await Task.Delay(_duration);
                _isAlreadyRunning = false;
                Debug.WriteLine("Released: " + DateTime.Now);
            }
        }

        public IObservableCommand RunInParallel( [Unit( "s", Second = 1 )] double duration )
        {
            // default behaviour is to run in parallel
            return ObservableCommands.Create( () => Task.Delay( TimeSpan.FromSeconds( duration ) ) );
        }


        public IObservableCommand RunQueued( [Unit( "s", Second = 1 )] double duration )
        {
            return new QueueingCommand(TimeSpan.FromSeconds(duration));
        }

        private class QueueingCommand : ObservableCommand
        {

            private static ConcurrentQueue<QueueingCommand> _queue = new ConcurrentQueue<QueueingCommand>();
            private static ManualResetEventSlim _waitEvent = new ManualResetEventSlim( false );
            private static Thread _queueThread;

            private TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
            private TimeSpan _duration;

            public QueueingCommand(TimeSpan duration)
            {
                _duration = duration;

                _queue.Enqueue( this );
                _waitEvent.Set();
            }

            public override async Task Run()
            {
                if (_waitEvent.IsSet)
                {
                    PushStateUpdate(0, _duration, CommandState.NotStarted);
                }
                await _taskCompletionSource.Task;
            }

            public async Task RunCore()
            {
                PushStateUpdate(0, _duration, CommandState.Running );
                await Task.Delay(_duration);
                _taskCompletionSource.TrySetResult( true );
            }

            public static void StartQueue()
            {
                _queueThread = new Thread(ExecuteQueuedCommands);
                _queueThread.Start();
            }

            public static void StopQueue()
            {
                _queue = null;
            }

            private static void ExecuteQueuedCommands()
            {
                ConcurrentQueue<QueueingCommand> queue;
                while ((queue = _queue) != null)
                {
                    while (queue.IsEmpty)
                    {
                        _waitEvent.Wait();
                    }
                    if (queue.TryDequeue(out var task))
                    {
                        _waitEvent.Reset();
                        task.RunCore().Wait();
                    }
                }
            }
        }

        public void Dispose()
        {
            QueueingCommand.StopQueue();
        }

        public MultiClientTest()
        {
            QueueingCommand.StartQueue();
        }
    }
}
