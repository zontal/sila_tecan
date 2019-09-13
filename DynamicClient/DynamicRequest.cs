namespace Tecan.Sila2.DynamicClient
{
    public class DynamicRequest : DynamicObjectProperty, ISilaRequestObject
    {
        public DynamicRequest(FeatureCommand command, string commandIdentifier, DataTypeType requestType)
            : base(command.Identifier, command.DisplayName, command.Description, requestType)
        {
            CommandIdentifier = commandIdentifier;
        }

        public string CommandIdentifier { get; }

        public string GetValidationErrors()
        {
            return null;
        }
    }
}
