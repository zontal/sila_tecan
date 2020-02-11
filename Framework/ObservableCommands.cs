using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes a facade to create observable commands from lambda expressions
    /// </summary>
    public static class ObservableCommands
    {
        /// <summary>
        /// Creates a new observable command for the given command delegate
        /// </summary>
        /// <param name="command">The command delegate</param>
        /// <returns>An observable command</returns>
        public static IObservableCommand Create( Func<Task> command )
        {
            return new DelegateCommand( updater => command() );
        }

        /// <summary>
        /// Creates a new observable command for the given command delegate
        /// </summary>
        /// <param name="command">The command delegate</param>
        /// <returns>An observable command</returns>
        public static IObservableCommand Create( Func<Action<double, TimeSpan, CommandState>, Task> command )
        {
            return new DelegateCommand(command);
        }

        /// <summary>
        /// Creates a new observable command for the given command delegate
        /// </summary>
        /// <typeparam name="T">The type of the command response</typeparam>
        /// <param name="command">The command delegate</param>
        /// <returns>An observable command</returns>
        public static IObservableCommand<T> Create<T>( Func<Action<double, TimeSpan, CommandState>, Task<T>> command )
        {
            return new DelegateCommand<T>( command );
        }

        /// <summary>
        /// Creates a new observable command for the given command delegate
        /// </summary>
        /// <typeparam name="TIntermediate">The type of intermediate responses</typeparam>
        /// <param name="command">The command delegate</param>
        /// <returns>An observable command</returns>
        public static IIntermediateObservableCommand<TIntermediate> Create<TIntermediate>( Func<Action<double, TimeSpan, CommandState>, Action<TIntermediate>, Task> command )
        {
            return new DelegateIntermediateCommand<TIntermediate>( command );
        }

        /// <summary>
        /// Creates a new observable command for the given command delegate
        /// </summary>
        /// <typeparam name="TIntermediate">The type of intermediate responses</typeparam>
        /// <typeparam name="T">The type of the command response</typeparam>
        /// <param name="command">The command delegate</param>
        /// <returns>An observable command</returns>
        public static IIntermediateObservableCommand<TIntermediate, T> Create<TIntermediate, T>( Func<Action<double, TimeSpan, CommandState>, Action<TIntermediate>, Task<T>> command )
        {
            return new DelegateIntermediateCommand<TIntermediate, T>( command );
        }

        /// <summary>
        /// Creates a new observable command for the given command delegate
        /// </summary>
        /// <param name="command">The command delegate</param>
        /// <returns>An observable command</returns>
        public static IObservableCommand Create( Func<CancellationToken, Task> command )
        {
            return new CancellationDelegateCommand( (updater, token) => command(token) );
        }

        /// <summary>
        /// Creates a new observable command for the given command delegate
        /// </summary>
        /// <param name="command">The command delegate</param>
        /// <returns>An observable command</returns>
        public static IObservableCommand Create( Func<Action<double, TimeSpan, CommandState>, CancellationToken, Task> command )
        {
            return new CancellationDelegateCommand( command );
        }

        /// <summary>
        /// Creates a new observable command for the given command delegate
        /// </summary>
        /// <typeparam name="T">The type of the command response</typeparam>
        /// <param name="command">The command delegate</param>
        /// <returns>An observable command</returns>
        public static IObservableCommand<T> Create<T>( Func<Action<double, TimeSpan, CommandState>, CancellationToken, Task<T>> command )
        {
            return new CancellationDelegateCommand<T>( command );
        }

        /// <summary>
        /// Creates a new observable command for the given command delegate
        /// </summary>
        /// <typeparam name="TIntermediate">The type of intermediate responses</typeparam>
        /// <param name="command">The command delegate</param>
        /// <returns>An observable command</returns>
        public static IIntermediateObservableCommand<TIntermediate> Create<TIntermediate>( Func<Action<double, TimeSpan, CommandState>, Action<TIntermediate>, CancellationToken, Task> command )
        {
            return new CancellationDelegateIntermediateCommand<TIntermediate>( command );
        }

        /// <summary>
        /// Creates a new observable command for the given command delegate
        /// </summary>
        /// <typeparam name="TIntermediate">The type of intermediate responses</typeparam>
        /// <typeparam name="T">The type of the command response</typeparam>
        /// <param name="command">The command delegate</param>
        /// <returns>An observable command</returns>
        public static IIntermediateObservableCommand<TIntermediate, T> Create<TIntermediate, T>( Func<Action<double, TimeSpan, CommandState>, Action<TIntermediate>, CancellationToken, Task<T>> command )
        {
            return new CancellationDelegateIntermediateCommand<TIntermediate, T>( command );
        }
    }
}
