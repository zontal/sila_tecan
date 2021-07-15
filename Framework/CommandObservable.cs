using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2
{
    /// <summary>
    /// Represents an observable based on a SiLA2 observable command
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CommandObservable<T> : IObservable<T>
    {
        private readonly IIntermediateObservableCommand<T> _innerCommand;
        private readonly List<IObserver<T>> _subscribers = new List<IObserver<T>>();

        /// <summary>
        /// Creates a new observable tracking the provided command
        /// </summary>
        /// <param name="innerCommand">The inner SiLA2 command</param>
        public CommandObservable(IIntermediateObservableCommand<T> innerCommand)
        {
            _innerCommand = innerCommand;

            ReadIntermediateResults();
        }

        private async void ReadIntermediateResults()
        {
            while(await _innerCommand.IntermediateValues.WaitToReadAsync())
            {
                if(_innerCommand.IntermediateValues.TryRead( out var result ))
                {
                    lock(_subscribers)
                    {
                        foreach(var observer in _subscribers)
                        {
                            observer.OnNext( result );
                        }
                    }
                }
            }
            lock(_subscribers)
            {
                foreach(var observer in _subscribers)
                {
                    observer.OnCompleted();
                }
            }
        }

        /// <inheritdoc />
        public IDisposable Subscribe( IObserver<T> observer )
        {
            _subscribers.Add( observer );
            return new Subscription( this, observer );
        }

        private class Subscription : IDisposable
        {
            private readonly CommandObservable<T> _parent;
            private readonly IObserver<T> _observer;

            public Subscription(CommandObservable<T> parent, IObserver<T> observer )
            {
                _parent = parent;
                _observer = observer;
            }

            public void Dispose()
            {
                lock(_parent._subscribers)
                {
                    _parent._subscribers.Remove( _observer );
                }
            }
        }
    }
}
