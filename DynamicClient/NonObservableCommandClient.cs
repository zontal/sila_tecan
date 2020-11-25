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
        }

        /// <summary>
        /// Invokes the command and returns a dynamic property that contains the response value
        /// </summary>
        /// <param name="args">The command request parameters</param>
        /// <param name="additionalMetadata">A dictionary with additional request metadata</param>
        /// <returns>A dynamic property with the response value</returns>
        public DynamicObjectProperty Invoke(DynamicRequest args, IDictionary<string, byte[]> additionalMetadata = null)
        {
            var callInfo = Context.ExecutionManager.CreateCallOptions(args.CommandIdentifier);
            try
            {
                var result = Context.ServerData.Channel.ExecuteUnobservableCommand( Context.ServiceName, Command.Identifier, args, MetadataHelper.TakeInMetadata(callInfo, additionalMetadata), RequestSerializer, ResponseSerializer );
                callInfo.FinishSuccessful();
                return result;
            }
            catch (Exception exception)
            {
                var convertedException = Context.ConvertException(exception);
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
        /// <param name="additionalMetadata">A dictionary with additional request metadata</param>
        /// <returns>A dynamic property with the response value</returns>
        public async Task<DynamicObjectProperty> InvokeAsync(DynamicRequest args, string host = null, IDictionary<string, byte[]> additionalMetadata = null )
        {
            var callInfo = Context.ExecutionManager.CreateCallOptions(args.CommandIdentifier);
            try
            {
                var result = await Context.ServerData.Channel.ExecuteUnobservableCommandAsync( Context.ServiceName, CommandIdentifier, args, MetadataHelper.TakeInMetadata( callInfo, additionalMetadata ), RequestSerializer, ResponseSerializer );
                callInfo.FinishSuccessful();
                return result;
            }
            catch (Exception exception)
            {
                var convertedException = Context.ConvertException(exception);
                callInfo.FinishWithErrors(convertedException);
                throw convertedException;
            }
        }
    }
}
