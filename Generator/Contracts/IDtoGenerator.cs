using System.CodeDom;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Generates data transfer objects
    /// </summary>
    public interface IDtoGenerator
    {
        /// <summary>
        /// Generates the data transfer objects for the given feature
        /// </summary>
        /// <param name="feature">The feature for which the data transfer objects should be generated</param>
        /// <param name="ns">The namespace in which the code shall be generated</param>
        /// <returns>A compilation unit with the generated code</returns>
        CodeCompileUnit GenerateInterfaceUnit(Feature feature, string ns);
    }
}