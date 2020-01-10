using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.Client;

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
                new Marshaller<DynamicRequest>(SerializeRequest, DeserializeRequest);
            var responseMarshaller =
                new Marshaller<DynamicObjectProperty>(SerializeResponse, DeserializeResponse);

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
        /// <param name="additionalMetadata">A dictionary with additional request metadata</param>
        /// <returns>A dynamic property with the response value</returns>
        public DynamicObjectProperty Invoke(DynamicRequest args, string host = null, IDictionary<string, byte[]> additionalMetadata = null)
        {
            var callInfo = Context.ExecutionManager.CreateCallOptions(args.CommandIdentifier);
            try
            {
                var result = Context.CallInvoker.BlockingUnaryCall(_method, host, MetadataHelper.TakeInMetadata( callInfo, additionalMetadata ), args);
                callInfo.FinishSuccessful();
                return result;
            }
            catch (Exception exception)
            {
                var convertedException = ErrorHandling.ConvertException(exception);
                callInfo.FinishWithErrors(convertedException);
                if( exception != convertedException )
                {
                    throw convertedException;
                }
                else
                {
                    // if the exception has not been converted, there is no need to override the stack trace
                    throw;
                }
            }
        }

        /// <summary>
        /// Invokes the command and returns a dynamic property that contains the response value
        /// </summary>
        /// <param name="args">The command request parameters</param>
        /// <param name="host">The host to target or null</param>
        /// <param name="additionalMetadata">A dictionary with additional request metadata</param>
        /// <returns>A dynamic property with the response value</returns>
        public async Task<DynamicObjectProperty> InvokeAsync(DynamicRequest args, string host = null, IDictionary<string, byte[]> additionalMetadata = null )
        {
            var callInfo = Context.ExecutionManager.CreateCallOptions(args.CommandIdentifier);
            try
            {
                var result = await Context.CallInvoker.AsyncUnaryCall(_method, host, MetadataHelper.TakeInMetadata( callInfo, additionalMetadata ), args);
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
