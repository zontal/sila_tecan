using System.CodeDom;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Generates client classes that implement a feature interface using SiLA
    /// </summary>
    public interface IClientGenerator
    {
        /// <summary>
        /// Generates the client for the given feature
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="ns">The namespace to which the code should be generated</param>
        /// <returns>A type declaration with the client implementation</returns>
        CodeCompileUnit GenerateClient( Feature feature, string ns );

        /// <summary>
        /// Generates the client for the given feature
        /// </summary>
        /// <param name="feature">The feature</param>
        CodeTypeDeclaration GenerateClientClass( Feature feature );
    }
}