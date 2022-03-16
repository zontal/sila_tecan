using System.CodeDom;
using System.Reflection;

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
        CodeTypeReference CreateFeatureInterfaceReference( Feature feature );

        /// <summary>
        /// Gets the name of the given command in code
        /// </summary>
        /// <param name="featureCommand">The command</param>
        /// <returns>The name of the corresponding method</returns>
        string GetCommandName( FeatureCommand featureCommand );

        /// <summary>
        /// Gets the name of the given property in code
        /// </summary>
        /// <param name="featureProperty">The property</param>
        /// <returns>The name of the corresponding property</returns>
        string GetPropertyName( FeatureProperty featureProperty );

        /// <summary>
        /// Creates a type reference to the given error identifier
        /// </summary>
        /// <param name="errorIdentifier"></param>
        /// <returns></returns>
        CodeTypeReference CreateExceptionReference( string errorIdentifier );

        /// <summary>
        /// Gets the name of the construct method for the given type name
        /// </summary>
        /// <param name="typeName">The name of the type</param>
        /// <returns>The name of a static construction method or null, if a regular constructor can be used</returns>
        string GetStaticConstructorMethod( string typeName );

        /// <summary>
        /// Gets the type of the member with the specified identifier
        /// </summary>
        /// <param name="identifier">The identifier of a member</param>
        /// <returns>A type reference to the member or null, if the type was not overridden</returns>
        CodeTypeReference GetMemberType( string identifier );

        /// <summary>
        /// Creates a type reference for the request DTO for the given command
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns>A type reference to the request DTO generated for this command</returns>
        CodeTypeReference GenerateCommandRequestType( FeatureCommand command );

        /// <summary>
        /// Creates a type reference for the response DTO for the given command
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns>A type reference to the response DTO generated for this command</returns>
        CodeTypeReference GenerateCommandResponseType( FeatureCommand command );

        /// <summary>
        /// Creates a type reference for the intermediate DTO for the given command
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns>A type reference to the response DTO generated for this command</returns>
        CodeTypeReference GenerateCommandIntermediateType( FeatureCommand command );

        /// <summary>
        /// Creates a type reference for the response DTO for the given property
        /// </summary>
        /// <param name="featureProperty">The property</param>
        /// <param name="typeTranslationProvider">The type translation provider in which context the property type is resolved</param>
        /// <returns>>A type reference to the response DTO generated for this property</returns>
        CodeTypeReference GetPropertyResponseType( FeatureProperty featureProperty, ITypeTranslationProvider typeTranslationProvider );

        /// <summary>
        /// Gets the observable command return type of the given command
        /// </summary>
        /// <param name="featureCommand">The feature command that shall be generated</param>
        /// <param name="typeTranslationProvider">The type translation provider in which context the property type is resolved</param>
        /// <returns>A code type reference for the interface type for the given command</returns>
        CodeTypeReference GetObservableCommandReturnType( FeatureCommand featureCommand, ITypeTranslationProvider typeTranslationProvider );

        /// <summary>
        /// Gets the intermediate type for the given feature command
        /// </summary>
        /// <param name="featureCommand">The feature command that shall be generated</param>
        /// <param name="typeTranslationProvider">The type translation provider in which context the property type is resolved</param>
        /// <returns>A code type reference for the intermediate type for the given command</returns>
        CodeTypeReference GetIntermediateType( FeatureCommand featureCommand, ITypeTranslationProvider typeTranslationProvider );

        /// <summary>
        /// Gets a non-standard method description, if registered
        /// </summary>
        /// <param name="command">The command for which the method is requested</param>
        /// <returns>A method info with the non-standard original method or null, if not applicable</returns>
        MethodInfo GetNonstandardMethod( FeatureCommand command );

        /// <summary>
        /// Gets a non-standard method description, if registered
        /// </summary>
        /// <param name="property">The property for which the method is requested</param>
        /// <returns>A method info with the non-standard original method or null, if not applicable</returns>
        MethodInfo GetNonstandardMethod( FeatureProperty property );
    }
}