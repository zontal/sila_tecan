using System.CodeDom;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Generates a server implementation for features
    /// </summary>
    public interface IServerGenerator
    {
        /// <summary>
        /// Generate a server implementation for the given feature
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="ns">The namespace to which the code should be generated</param>
        /// <returns>A declaration of the server class</returns>
        CodeCompileUnit GenerateServer( Feature feature, string ns );

        /// <summary>
        /// Generates an abstract base class for a metadata interceptor for the given metadata
        /// </summary>
        /// <param name="feature">The feature for which an interceptor should be generated</param>
        /// <param name="metadata">The metadata for which an interceptor should be generated</param>
        /// <returns>A declaration of the interceptor class</returns>
        CodeTypeDeclaration GenerateMetadataInterceptorBase( Feature feature, FeatureMetadata metadata );

        /// <summary>
        /// Generate a server implementation for the given feature
        /// </summary>
        /// <param name="feature">The feature</param>
        CodeTypeDeclaration GenerateServerClass( Feature feature );
    }
}