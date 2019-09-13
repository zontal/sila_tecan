using System.CodeDom;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Generates interfaces from features
    /// </summary>
    public interface IInterfaceGenerator
    {
        /// <summary>
        /// Generates an interface for the given feature
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="ns">The namespace to which the code should be generated. If null, a combination of feature originator and use case is used.</param>
        /// <returns>A code compilation unit containing the code for the feature and derived types.</returns>
        CodeCompileUnit GenerateInterfaceUnit(Feature feature, string ns);
    }
}