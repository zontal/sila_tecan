using System;
using System.CodeDom;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Denotes an interface for a hook into the code generator
    /// </summary>
    public interface IGeneratorHook
    {
        /// <summary>
        /// Called when a server class is generated for a feature
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="compileUnit">The generated code</param>
        /// <param name="generatedType">A declaration of the server class</param>
        void OnServerGenerated(Feature feature, CodeTypeDeclaration generatedType, CodeCompileUnit compileUnit );

        /// <summary>
        /// Called when a client class is generated for a feature
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="compileUnit">The generated code</param>
        /// <param name="generatedType">A declaration of the server class</param>
        void OnClientGenerated(Feature feature, CodeTypeDeclaration generatedType, CodeCompileUnit compileUnit );

        /// <summary>
        /// Called when a feature is generated for the given type
        /// </summary>
        /// <param name="interfaceType">The interface for which the feature was generated</param>
        /// <param name="feature">The feature that was generated</param>
        void OnFeatureGenerated(Type interfaceType, Feature feature);

        /// <summary>
        /// Called when an interface is generated for the given type
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="generatedType">A declaration of the interface</param>
        /// <param name="compileUnit">The generated code</param>
        void OnInterfaceGenerated( Feature feature, CodeTypeDeclaration generatedType, CodeCompileUnit compileUnit );
    }
}
