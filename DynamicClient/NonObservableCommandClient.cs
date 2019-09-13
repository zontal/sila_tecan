using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Tecan.Sila2.DynamicClient
{
    public class NonObservableCommandClient : CommandClient
    {
        private readonly Method<DynamicRequest, DynamicObjectProperty> _method;

        public NonObservableCommandClient(FeatureCommand command, FeatureContext context) : base(command, context)
        {
            var requestMarshaller =
                new Marshaller<DynamicRequest>(context.Serializer.Serialize, DeserializeRequest);
            var responseMarshaller =
                new Marshaller<DynamicObjectProperty>(context.Serializer.Serialize, DeserializeResponse);

            _method = new Method<DynamicRequest, DynamicObjectProperty>(
                MethodType.Unary,
                context.ServiceName,
                command.Identifier,
                requestMarshaller,
                responseMarshaller);
        }

        public DynamicObjectProperty Invoke(DynamicRequest args, string host = null)
        {
            var callInfo = Context.ExecutionManager.CreateCallOptions(args.CommandIdentifier);
            try
            {
                var result = Context.CallInvoker.BlockingUnaryCall(_method, host, callInfo.Options, args);
                callInfo.FinishSuccessful();
                return result;
            }
            catch (Exception exception)
            {
                var convertedException = ErrorHandling.ConvertException(exception);
                callInfo.FinishWithErrors(convertedException);
                throw convertedException;
            }
        }

        public async Task<DynamicObjectProperty> InvokeAsync(DynamicRequest args, string host = null)
        {
            var callInfo = Context.ExecutionManager.CreateCallOptions(args.CommandIdentifier);
            try
            {
                var result = await Context.CallInvoker.AsyncUnaryCall(_method, host, callInfo.Options, args);
                callInfo.FinishSuccessful();
                return result;
            }
            catch (Exception exception)
            {
                var convertedException = ErrorHandling.ConvertException(exception);
                callInfo.FinishWithErrors(convertedException);
                throw convertedException;
            }
        }
    }
}
