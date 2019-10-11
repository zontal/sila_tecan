using System;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Denotes the class for a dynamic request
    /// </summary>
    public class DynamicRequest : DynamicObjectProperty, ISilaRequestObject
    {
        /// <summary>
        /// Creates a new dynamic request for the given command
        /// </summary>
        /// <param name="command">The feature command</param>
        /// <param name="commandIdentifier">The fully qualified command identifier</param>
        /// <param name="requestType">The type of the request</param>
        public DynamicRequest(FeatureCommand command, string commandIdentifier, DataTypeType requestType)
            : base((command ?? throw new ArgumentNullException(nameof(command))).Identifier, command.DisplayName, command.Description, requestType)
        {
            CommandIdentifier = commandIdentifier;
        }

        /// <summary>
        /// Gets the fully qualified command identifier
        /// </summary>
        public string CommandIdentifier { get; }

        /// <summary>
        /// Gets any validation errors
        /// </summary>
        /// <returns></returns>
        public string GetValidationErrors()
        {
            return null;
        }
    }
}
