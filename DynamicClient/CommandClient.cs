namespace Tecan.Sila2.DynamicClient
{
    public abstract class CommandClient
    {
        protected readonly FeatureContext Context;
        protected readonly FeatureCommand Command;

        public string CommandIdentifier => Context.Feature.GetFullyQualifiedIdentifier(Command);

        protected readonly DataTypeType RequestType;
        protected readonly DataTypeType ResponseType;

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

        public DynamicRequest CreateRequest()
        {
            return new DynamicRequest(Command, CommandIdentifier, RequestType);
        }

        public DynamicObjectProperty CreateResponse()
        {
            return new DynamicObjectProperty(Command.Identifier, Command.DisplayName, Command.Description, ResponseType);
        }

        protected DynamicObjectProperty DeserializeResponse(byte[] arg)
        {
            var response = CreateResponse();
            Context.Serializer.Deserialize(response, arg, false);
            return response;
        }

        protected DynamicRequest DeserializeRequest(byte[] arg)
        {
            var request = CreateRequest();
            Context.Serializer.Deserialize(request, arg, false);
            return request;
        }
    }
}
