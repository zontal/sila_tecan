using System.CodeDom;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Describes a component that provides names for feature elements
    /// </summary>
    public interface ICodeNameProvider
    {
        /// <summary>
        /// Creates a type reference for the interface representing the given feature
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <returns>A type reference to the interface</returns>
        CodeTypeReference CreateFeatureInterfaceReference(Feature feature);

        /// <summary>
        /// Gets the name of the given command in code
        /// </summary>
        /// <param name="featureCommand">The command</param>
        /// <returns>The name of the corresponding method</returns>
        string GetCommandName(FeatureCommand featureCommand);

        /// <summary>
        /// Gets the name of the given property in code
        /// </summary>
        /// <param name="featureProperty">The property</param>
        /// <returns>The name of the corresponding property</returns>
        string GetPropertyName(FeatureProperty featureProperty);

        /// <summary>
        /// Creates a type reference to the given error identifier
        /// </summary>
        /// <param name="errorIdentifier"></param>
        /// <returns></returns>
        CodeTypeReference CreateExceptionReference(string errorIdentifier);

        /// <summary>
        /// Gets the name of the construct method for the given type name
        /// </summary>
        /// <param name="typeName">The name of the type</param>
        /// <returns>The name of a static construction method or null, if a regular constructor can be used</returns>
        string GetStaticConstructorMethod(string typeName);

        /// <summary>
        /// Gets the type of the member with the specified identifier
        /// </summary>
        /// <param name="identifier">The identifier of a member</param>
        /// <returns>A type reference to the member or null, if the type was not overridden</returns>
        CodeTypeReference GetMemberType(string identifier);

        /// <summary>
        /// Creates a type reference for the request DTO for the given command
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns>A type reference to the request DTO generated for this command</returns>
        CodeTypeReference GenerateCommandRequestType(FeatureCommand command);

        /// <summary>
        /// Creates a type reference for the response DTO for the given command
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns>A type reference to the response DTO generated for this command</returns>
        CodeTypeReference GenerateCommandResponseType(FeatureCommand command);

        /// <summary>
        /// Creates a type reference for the intermediate DTO for the given command
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns>A type reference to the response DTO generated for this command</returns>
        CodeTypeReference GenerateCommandIntermediateType(FeatureCommand command);
    }
}