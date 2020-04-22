using System;
using System.Linq;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Class for a dynamic command client
    /// </summary>
    public abstract class CommandClient
    {
        /// <summary>
        /// The feature context in which the command is connected
        /// </summary>
        protected readonly FeatureContext Context;

        /// <summary>
        /// The command model for the command
        /// </summary>
        protected readonly FeatureCommand Command;

        /// <summary>
        /// Gets the command identifier that this client is used for
        /// </summary>
        public string CommandIdentifier => Context.Feature.GetFullyQualifiedIdentifier(Command);

        /// <summary>
        /// The type for request parameters of the command
        /// </summary>
        protected readonly DataTypeType RequestType;

        /// <summary>
        /// The type for responses of the command
        /// </summary>
        protected readonly DataTypeType ResponseType;

        private readonly string _parameterWithBinary;

        /// <summary>
        /// Creates a new command client for the given feature context
        /// </summary>
        /// <param name="command">The command client</param>
        /// <param name="context">The feature context</param>
        protected CommandClient(FeatureCommand command, FeatureContext context)
        {
            Context = context;
            Command = command;

            RequestType = new DataTypeType()
            {
                Item = new StructureType()
                {
                    Element = command.Parameter
                }
            };
            ResponseType = new DataTypeType()
            {
                Item = new StructureType()
                {
                    Element = command.Response
                }
            };
            var parameterWithBinary = command.Parameter?.FirstOrDefault( p => IsBinaryType( p.DataType, context.ResolveType ) );
            if( parameterWithBinary != null )
            {
                _parameterWithBinary = context.Feature.GetFullyQualifiedParameterIdentifier( command, parameterWithBinary.Identifier );
            }
        }

        private bool IsBinaryType( DataTypeType type, Func<string, DataTypeType> resolveType )
        {
            switch( type.Item )
            {
                case BasicType basicType:
                    return basicType == BasicType.Binary;
                case StructureType structureType:
                    return structureType.Element != null && structureType.Element.Any( e => IsBinaryType( e.DataType, resolveType ) );
                case ListType listType:
                    return IsBinaryType( listType.DataType, resolveType );
                case ConstrainedType constrained:
                    return IsBinaryType( constrained.DataType, resolveType );
                case string typeName:
                    return IsBinaryType( resolveType( typeName ), resolveType );
            }

            throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// Creates a request object for the command
        /// </summary>
        /// <returns>A dynamic request object</returns>
        public DynamicRequest CreateRequest()
        {
            return new DynamicRequest(Command, CommandIdentifier, RequestType);
        }

        /// <summary>
        /// Creates an empty response object
        /// </summary>
        /// <returns></returns>
        protected DynamicObjectProperty CreateResponse()
        {
            return new DynamicObjectProperty(Command.Identifier, Command.DisplayName, Command.Description, ResponseType);
        }

        /// <summary>
        /// Serializes the dynamic request 
        /// </summary>
        /// <param name="value">The dynamic request</param>
        /// <returns>A byte array with the data</returns>
        protected byte[] SerializeRequest( DynamicObjectProperty value )
        {
            var store = _parameterWithBinary != null ? Context.ExecutionManager.CreateBinaryStore( _parameterWithBinary ) : null;
            return Context.Serializer.Serialize( value, store );
        }

        /// <summary>
        /// Serializes the dynamic response 
        /// </summary>
        /// <param name="value">The dynamic response</param>
        /// <returns>A byte array with the data</returns>
        protected byte[] SerializeResponse( DynamicObjectProperty value )
        {
            return Context.Serializer.Serialize( value, Context.ExecutionManager.DownloadBinaryStore );
        }

        /// <summary>
        /// Deserializes the given byte array as command response
        /// </summary>
        /// <param name="arg">The data array</param>
        /// <returns>A dynamic data that represents the command response</returns>
        protected DynamicObjectProperty DeserializeResponse(byte[] arg)
        {
            var response = CreateResponse();
            Context.Serializer.Deserialize(response, arg, false, Context.ExecutionManager.DownloadBinaryStore);
            return response;
        }

        /// <summary>
        /// Deserializes the given byte array as command request
        /// </summary>
        /// <param name="arg">The data array</param>
        /// <returns>A dynamic data that represents the command request</returns>
        protected DynamicRequest DeserializeRequest(byte[] arg)
        {
            var request = CreateRequest();
            var store = _parameterWithBinary != null ? Context.ExecutionManager.CreateBinaryStore( _parameterWithBinary ) : null;
            Context.Serializer.Deserialize(request, arg, false, store);
            return request;
        }
    }
}
