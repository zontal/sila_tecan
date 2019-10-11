using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Denotes a dynamic client for an unobservable command
    /// </summary>
    public class NonObservableCommandClient : CommandClient
    {
        private readonly Method<DynamicRequest, DynamicObjectProperty> _method;

        /// <summary>
        /// Creates a client for an unobservable command
        /// </summary>
        /// <param name="command">The command model</param>
        /// <param name="context">The feature context</param>
        public NonObservableCommandClient(FeatureCommand command, FeatureContext context) : base(command, context)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (command.Observable == FeatureCommandObservable.Yes) throw new ArgumentException("The command is observable.", nameof(command));

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

        /// <summary>
        /// Invokes the command and returns a dynamic property that contains the response value
        /// </summary>
        /// <param name="args">The command request parameters</param>
        /// <param name="host">The host to target or null</param>
        /// <returns>A dynamic property with the response value</returns>
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

        /// <summary>
        /// Invokes the command and returns a dynamic property that contains the response value
        /// </summary>
        /// <param name="args">The command request parameters</param>
        /// <param name="host">The host to target or null</param>
        /// <returns>A dynamic property with the response value</returns>
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
