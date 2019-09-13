using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.DynamicClient
{
    public class ObservableCommandClient : CommandClient
    {
        private readonly Method<DynamicRequest, CommandConfirmation> _method;
        private readonly Method<CommandExecutionUuid, ExecutionState> _stateMethod;
        private readonly Method<CommandExecutionUuid, DynamicObjectProperty> _resultMethod;

        public ObservableCommandClient(FeatureCommand command, FeatureContext context) : base(command, context)
        {
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
                command.Identifier + "_State",
                ProtobufMarshaller<CommandExecutionUuid>.Default,
                ProtobufMarshaller<ExecutionState>.Default);
            _resultMethod = new Method<CommandExecutionUuid, DynamicObjectProperty>(
                MethodType.Unary,
                context.ServiceName,
                command.Identifier + "_Result",
                ProtobufMarshaller<CommandExecutionUuid>.Default,
                responseMarshaller);
        }

        public IObservableCommand<DynamicObjectProperty> Invoke(DynamicRequest args, string host = null)
        {
            return ObservableCommand.Create(Context.ExecutionManager.CreateCallOptions(args.CommandIdentifier), Context.CallInvoker, args, _method, _stateMethod, _resultMethod, dn => dn, Context.ConvertException);
        }
    }
}
