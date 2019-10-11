using System;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Denotes a client for an observable command
    /// </summary>
    public class ObservableCommandClient : CommandClient
    {
        private readonly Method<DynamicRequest, CommandConfirmation> _method;
        private readonly Method<CommandExecutionUuid, ExecutionState> _stateMethod;
        private readonly Method<CommandExecutionUuid, DynamicObjectProperty> _resultMethod;

        /// <summary>
        /// Creates a new client for an observable command
        /// </summary>
        /// <param name="command">The command for which the client can be used</param>
        /// <param name="context">The context in which the command is called</param>
        public ObservableCommandClient(FeatureCommand command, FeatureContext context) : base(command, context)
        {
            if(command == null) throw new ArgumentNullException( nameof( command ) );
            if(context == null) throw new ArgumentNullException( nameof( context ) );
            if(command.Observable == FeatureCommandObservable.No) throw new ArgumentException( "The command is not observable.", nameof( command ) );

            var requestMarshaller =
                new Marshaller<DynamicRequest>(context.Serializer.Serialize, DeserializeRequest);
            var responseMarshaller =
                new Marshaller<DynamicObjectProperty>(context.Serializer.Serialize, DeserializeResponse);

            _method = new Method<DynamicRequest, CommandConfirmation>(
                MethodType.Unary,
                context.ServiceName,
                command.Identifier,
                requestMarshaller,
                ProtobufMarshaller<CommandConfirmation>.Default);
            _stateMethod = new Method<CommandExecutionUuid, ExecutionState>(
                MethodType.ServerStreaming,
                context.ServiceName,
                command.Identifier + "_Info",
                ProtobufMarshaller<CommandExecutionUuid>.Default,
                ProtobufMarshaller<ExecutionState>.Default);
            _resultMethod = new Method<CommandExecutionUuid, DynamicObjectProperty>(
                MethodType.Unary,
                context.ServiceName,
                command.Identifier + "_Result",
                ProtobufMarshaller<CommandExecutionUuid>.Default,
                responseMarshaller);
        }

        /// <summary>
        /// Invokes the command
        /// </summary>
        /// <param name="args">The command request parameters</param>
        /// <param name="host">The host to which the request should be sent or null to send it to the default host</param>
        /// <returns>An observable command object</returns>
        public IObservableCommand<DynamicObjectProperty> Invoke(DynamicRequest args, string host = null)
        {
            return ObservableCommand.Create(Context.ExecutionManager.CreateCallOptions(args.CommandIdentifier), Context.CallInvoker, args, _method, _stateMethod, _resultMethod, dn => dn, Context.ConvertException);
        }
    }
}
