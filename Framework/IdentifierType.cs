namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes types of identifiers
    /// </summary>
    public enum IdentifierType
    {
        /// <summary>
        /// Identifier of a feature
        /// </summary>
        FeatureIdentifier,
        /// <summary>
        /// Identifier of a command
        /// </summary>
        CommandIdentifier,
        /// <summary>
        /// Identifier of a command parameter
        /// </summary>
        CommandParameterIdentifier,
        /// <summary>
        /// Identifier of a command response
        /// </summary>
        CommandResponseIdentifier,
        /// <summary>
        /// Identifier of an error
        /// </summary>
        DefinedExecutionErrorIdentifier,
        /// <summary>
        /// Identifier of a property
        /// </summary>
        PropertyIdentifier,
        /// <summary>
        /// Identifier of a type
        /// </summary>
        TypeIdentifier,
        /// <summary>
        /// Identifier of an intermediate response
        /// </summary>
        IntermediateResponseIdentifier,
        /// <summary>
        /// Identifier of a custom metadata
        /// </summary>
        MetadataIdentifier
    }
}
