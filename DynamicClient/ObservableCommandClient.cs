using System;
using System.Collections.Generic;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Denotes a client for an observable command
    /// </summary>
    public class ObservableCommandClient : CommandClient
    {
        /// <summary>
        /// Remote method for the SiLA-Command
        /// </summary>
        protected readonly Method<DynamicRequest, CommandConfirmation> Method;

        /// <summary>
        /// Remote method for the SiLA-Command's State/Info stream
        /// </summary>
        protected readonly Method<CommandExecutionUuid, ExecutionState> StateMethod;

        /// <summary>
        /// Remote method for the SiLA-Command's Results
        /// </summary>
        protected readonly Method<CommandExecutionUuid, DynamicObjectProperty> ResultMethod;

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
        }

        /// <summary>
        /// Invokes the command
        /// </summary>
        /// <param name="args">The command request parameters</param>
        /// <param name="additionalMetadata">A dictionary with additional request metadata</param>
        /// <returns>An observable command object</returns>
        public DynamicCommand Invoke( DynamicRequest args, IDictionary<string, byte[]> additionalMetadata = null )
        {
            try
            {
                var callInfo = MetadataHelper.TakeInMetadata( Context.ExecutionManager.CreateCallOptions( args.CommandIdentifier ), additionalMetadata );

                var actualCommand = Context.Channel.ExecuteObservableCommand( Context.ServiceName, Command.Identifier, args, dn => dn, Context.ConvertException, callInfo, RequestSerializer, ResponseSerializer );

                return new DynamicCommand( actualCommand, Context );
            }
            catch( Exception exception )
            {
                var converted = ErrorHandling.ConvertException( exception );
                if( exception == converted )
                {
                    throw;
                }
                else
                {
                    throw converted;
                }
            }
        }
    }

    /// <summary>
    /// Denotes a client for an observable command that creates IntermediateResponses
    /// </summary>
    public class IntermediateObservableCommandClient : ObservableCommandClient {

        /// <summary>
        /// The type for intermediate responses of the command
        /// </summary>
        protected readonly DataTypeType IntermediateType;

        /// <summary>
        /// Remote method for the SiLA-Command's IntermediateResults stream
        /// </summary>
        private readonly ByteSerializer<DynamicObjectProperty> _intermediateSerializer;

        /// <summary>
        /// Creates a new client for an observable command that creates IntermediateResponses
        /// </summary>
        /// <param name="command">The command for which the client can be used</param>
        /// <param name="context">The context in which the command is called</param>
        public IntermediateObservableCommandClient(FeatureCommand command, FeatureContext context) : base(command, context)
        {
            IntermediateType = new DataTypeType()
            {
                Item = new StructureType()
                {
                    Element = command.IntermediateResponse
                }
            };

            _intermediateSerializer =
                new ByteSerializer<DynamicObjectProperty>(SerializeResponse, DeserializeIntermediate);
        }

        /// <summary>
        /// Creates an empty intermediate response object
        /// </summary>
        /// <returns></returns>
        protected DynamicObjectProperty CreateIntermediate()
        {
            return new DynamicObjectProperty(Command.Identifier, Command.DisplayName, Command.Description, IntermediateType);
        }

        /// <summary>
        /// Deserializes the given byte array as command intermediate response
        /// </summary>
        /// <param name="arg">The data array</param>
        /// <returns>A dynamic data that represents the command request</returns>
        protected DynamicObjectProperty DeserializeIntermediate(byte[] arg)
        {
            var intermediate = CreateIntermediate();
            Context.Serializer.Deserialize(intermediate, arg, false, Context.ExecutionManager.DownloadBinaryStore);
            return intermediate;
        }

        /// <summary>
        /// Invokes the command
        /// </summary>
        /// <param name="args">The command request parameters</param>
        /// <param name="additionalMetadata">A dictionary with additional request metadata</param>
        /// <returns>An observable command object</returns>
        public new IIntermediateObservableCommand<DynamicObjectProperty, DynamicObjectProperty> Invoke( DynamicRequest args, IDictionary<string, byte[]> additionalMetadata = null )
        {
            try
            {
                var callInfo = MetadataHelper.TakeInMetadata( Context.ExecutionManager.CreateCallOptions( args.CommandIdentifier ), additionalMetadata );


                var actualCommand = Context.Channel.ExecuteIntermediatesCommand( Context.ServiceName, Command.Identifier, args, dn => dn, dn => dn, Context.ConvertException, callInfo, RequestSerializer, _intermediateSerializer, ResponseSerializer );

                return new DynamicIntermediateCommand( actualCommand, Context );
            }
            catch( Exception exception )
            {
                var converted = ErrorHandling.ConvertException( exception );
                if( exception == converted )
                {
                    throw;
                }
                else
                {
                    throw converted;
                }
            }
        }
    }
}
