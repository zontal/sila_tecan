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

        /// <summary>
        /// Creates a new command client for the given feature context
        /// </summary>
        /// <param name="command">The command client</param>
        /// <param name="context">The feature context</param>
        public CommandClient(FeatureCommand command, FeatureContext context)
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
        /// Deserializes the given byte array as command response
        /// </summary>
        /// <param name="arg">The data array</param>
        /// <returns>A dynamic data that represents the command response</returns>
        protected DynamicObjectProperty DeserializeResponse(byte[] arg)
        {
            var response = CreateResponse();
            Context.Serializer.Deserialize(response, arg, false);
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
            Context.Serializer.Deserialize(request, arg, false);
            return request;
        }
    }
}
