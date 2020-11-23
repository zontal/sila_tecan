using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Denotes a command that is executed on a remote server
    /// </summary>
    public interface IClientCommand : IObservableCommand
    {
        /// <summary>
        /// Gets the call invoker for the command
        /// </summary>
        CallInvoker CallInvoker
        {
            get;
        }

        /// <summary>
        /// Gets the command identifier
        /// </summary>
        CommandExecutionUuid CommandId
        {
            get;
        }

        /// <summary>
        /// Gets the call options for the command
        /// </summary>
        CallOptions CreateCallOptions();
    }
}
