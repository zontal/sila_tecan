using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tecan.Sila2
{
    internal class CancellationDelegateCommand : ObservableCommand
    {
        private readonly Func<Action<double, TimeSpan, CommandState>, CancellationToken, Task> _command;

        public CancellationDelegateCommand( Func<Action<double, TimeSpan, CommandState>, CancellationToken, Task> command )
            : base(new CancellationTokenSource())
        {
            _command = command;
        }

        public override Task Run()
        {
            return _command( PushStateUpdate, CancellationToken );
        }
    }

    internal class CancellationDelegateCommand<T> : ObservableCommand<T>
    {
        private readonly Func<Action<double, TimeSpan, CommandState>, CancellationToken, Task<T>> _command;

        public CancellationDelegateCommand( Func<Action<double, TimeSpan, CommandState>, CancellationToken, Task<T>> command )
            : base( new CancellationTokenSource() )
        {
            _command = command;
        }

        public override Task<T> Run()
        {
            return _command( PushStateUpdate, CancellationToken );
        }
    }

    internal class CancellationDelegateIntermediateCommand<TIntermediate> : ObservableIntermediatesCommand<TIntermediate>
    {
        private readonly Func<Action<double, TimeSpan, CommandState>, Action<TIntermediate>, CancellationToken, Task> _command;

        public CancellationDelegateIntermediateCommand( Func<Action<double, TimeSpan, CommandState>, Action<TIntermediate>, CancellationToken, Task> command )
            : base( new CancellationTokenSource() )
        {
            _command = command;
        }

        public override Task Run()
        {
            return _command( PushStateUpdate, PushIntermediate, CancellationToken );
        }
    }

    internal class CancellationDelegateIntermediateCommand<TIntermediate, T> : ObservableIntermediatesCommand<TIntermediate, T>
    {
        private readonly Func<Action<double, TimeSpan, CommandState>, Action<TIntermediate>, CancellationToken, Task<T>> _command;

        public CancellationDelegateIntermediateCommand( Func<Action<double, TimeSpan, CommandState>, Action<TIntermediate>, CancellationToken, Task<T>> command )
            : base( new CancellationTokenSource() )
        {
            _command = command;
        }

        public override Task<T> Run()
        {
            return _command( PushStateUpdate, PushIntermediate, CancellationToken );
        }
    }
}
