using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// This class extents the Feature class auto-generated from the FeatureDefinition schema.
    /// </summary>
    public partial class Feature
    {
        /// <summary>
        /// Gets the fully qualified identifier assembled from several feature properties as specified in the SiLA 2 standard.
        /// </summary>
        public string FullyQualifiedIdentifier => $"{this.Originator}/{(this.Category != null ? this.Category + "/" : string.Empty)}{this.Identifier}/v{Version.Parse(this.FeatureVersion).Major}";


        /// <summary>
        /// Gets the namespace to be used when calling the gRPC service.
        /// </summary>
        public string Namespace => $"sila2.{Originator}.{Category}.{Identifier.ToLower()}.v{Version.Parse(this.FeatureVersion).Major}";

        /// <summary>
        /// Gets the fully qualified identifier of the given command
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns>The fully qualified identifier of the command</returns>
        public string GetFullyQualifiedIdentifier(FeatureCommand command)
        {
            return FullyQualifiedIdentifier + "/Command/" + command.Identifier;
        }

        /// <summary>
        /// Gets the fully qualified identifier of the given property
        /// </summary>
        /// <param name="property">The property</param>
        /// <returns>The fully qualified identifier of the property</returns>
        public string GetFullyQualifiedIdentifier(FeatureProperty property)
        {
            return FullyQualifiedIdentifier + "/Property/" + property.Identifier;
        }

        /// <summary>
        /// Gets the fully qualified identifier of the given client metadata
        /// </summary>
        /// <param name="metadata">The client metadata</param>
        /// <returns>The fully qualified identifier</returns>
        public string GetFullyQualifiedIdentifier(FeatureMetadata metadata)
        {
            return FullyQualifiedIdentifier + "/Metadata/" + metadata.Identifier;
        }

        /// <summary>
        /// Gets the fully qualified identifier of the given defined execution error
        /// </summary>
        /// <param name="error">The defined execution error</param>
        /// <returns>The fully qualified identifier of the error</returns>
        public string GetFullyQualifiedIdentifier(FeatureDefinedExecutionError error)
        {
            return FullyQualifiedIdentifier + "/DefinedError/" + error.Identifier;
        }

        /// <summary>
        /// Gets the fully qualified identifier of the given command parameter
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="parameter">The identifier of the command parameter</param>
        /// <returns>The fully qualified identifier of the command parameter</returns>
        public string GetFullyQualifiedParameterIdentifier( FeatureCommand command, string parameter )
        {
            return GetFullyQualifiedIdentifier( command ) + "/Parameter/" + parameter;
        }

        /// <summary>
        /// Gets the fully qualified identifier of the given command response
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="parameter">The identifier of the command response</param>
        /// <returns>The fully qualified identifier of the command response</returns>
        public string GetFullyQualifiedResponseIdentifier( FeatureCommand command, string parameter )
        {
            return GetFullyQualifiedIdentifier( command ) + "/Response/" + parameter;
        }
        /// <summary>
        /// Gets the fully qualified identifier of the given command intermediate response
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="parameter">The identifier of the command intermediate response</param>
        /// <returns>The fully qualified identifier of the command intermediate response</returns>
        public string GetFullyQualifiedIntermediateIdentifier( FeatureCommand command, string parameter )
        {
            return GetFullyQualifiedIdentifier( command ) + "/IntermediateResponse/" + parameter;
        }
    }
}