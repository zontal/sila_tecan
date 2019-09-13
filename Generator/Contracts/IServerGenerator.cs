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
        /// <returns>A declaration of the server class</returns>
        CodeTypeDeclaration GenerateServer(Feature feature);

        /// <summary>
        /// Generates an abstract base class for a metadata interceptor for the given metadata
        /// </summary>
        /// <param name="feature">The feature for which an interceptor should be generated</param>
        /// <param name="metadata">The metadata for which an interceptor should be generated</param>
        /// <returns>A declaration of the interceptor class</returns>
        CodeTypeDeclaration GenerateMetadataInterceptorBase(Feature feature, FeatureMetadata metadata);
    }
}