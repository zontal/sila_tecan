using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2
{
    /// <summary>
    /// Represents a SiLA2 observable command based on an observable
    /// </summary>
    /// <typeparam name="T">The type of the observable</typeparam>
    public class ObservableRxCommand<T> : ObservableIntermediatesCommand<T>, IObserver<T>
    {
        private IDisposable _subscription;
        private readonly TaskCompletionSource<bool> _taskCompletionSource;

        /// <summary>
        /// Creates a new command for the given observable
        /// </summary>
        /// <param name="observable">The observable</param>
        public ObservableRxCommand( IObservable<T> observable ) : base()
        {
            _subscription = observable.Subscribe( this );
            _taskCompletionSource = new TaskCompletionSource<bool>();
        }

        /// <inheritdoc />
        public override void Cancel()
        {
            _taskCompletionSource.TrySetCanceled();
            _subscription?.Dispose();
            _subscription = null;
            base.Cancel();
        }

        /// <inheritdoc />
        protected override void Cleanup( Exception exception )
        {
            base.Cleanup( exception );
            _subscription?.Dispose();
            _subscription = null;
        }

        /// <inheritdoc />
        public void OnCompleted()
        {
            _taskCompletionSource.TrySetResult( true );
        }

        /// <inheritdoc />
        public void OnError( Exception error )
        {
            _taskCompletionSource.SetException( error );
        }

        /// <inheritdoc />
        public void OnNext( T value )
        {
            PushIntermediate( value );
        }

        /// <inheritdoc />
        public override Task Run()
        {
            return _taskCompletionSource.Task;
        }
    }
}
