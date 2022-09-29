using System;
using System.Threading.Tasks;

namespace Tecan.Sila2
{
    internal class DelegateCommand : ObservableCommand
    {
        private readonly Func<Action<double, TimeSpan, CommandState>, Task> _command;

        public DelegateCommand( Func<Action<double, TimeSpan, CommandState>, Task> command )
        {
            _command = command;
        }

        public override Task Run()
        {
            PushStateUpdate( 0, TimeSpan.Zero, CommandState.Running );
            return _command( PushStateUpdate );
        }
    }

    internal class DelegateCommand<T> : ObservableCommand<T>
    {
        private readonly Func<Action<double, TimeSpan, CommandState>, Task<T>> _command;

        public DelegateCommand( Func<Action<double, TimeSpan, CommandState>, Task<T>> command )
        {
            _command = command;
        }

        public override Task<T> Run()
        {
            PushStateUpdate( 0, TimeSpan.Zero, CommandState.Running );
            return _command( PushStateUpdate );
        }
    }

    internal class DelegateIntermediateCommand<TIntermediate> : ObservableIntermediatesCommand<TIntermediate>
    {
        private readonly Func<Action<double, TimeSpan, CommandState>, Action<TIntermediate>, Task> _command;

        public DelegateIntermediateCommand( Func<Action<double, TimeSpan, CommandState>, Action<TIntermediate>, Task> command )
        {
            _command = command;
        }

        public override Task Run()
        {
            PushStateUpdate( 0, TimeSpan.Zero, CommandState.Running );
            return _command( PushStateUpdate, PushIntermediate );
        }
    }

    internal class DelegateIntermediateCommand<TIntermediate, T> : ObservableIntermediatesCommand<TIntermediate, T>
    {
        private readonly Func<Action<double, TimeSpan, CommandState>, Action<TIntermediate>, Task<T>> _command;

        public DelegateIntermediateCommand( Func<Action<double, TimeSpan, CommandState>, Action<TIntermediate>, Task<T>> command )
        {
            _command = command;
        }

        public override Task<T> Run()
        {
            PushStateUpdate( 0, TimeSpan.Zero, CommandState.Running );
            return _command( PushStateUpdate, PushIntermediate );
        }
    }
}
