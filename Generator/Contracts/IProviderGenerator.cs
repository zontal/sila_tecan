using System.CodeDom;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Generates providers for features
    /// </summary>
    public interface IProviderGenerator
    {
        /// <summary>
        /// Generates a service provider for the given feature
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="ns">The namespace to which the provider should be generated</param>
        /// <param name="generateServer">True, if a server should be generated, otherwise False</param>
        /// <param name="generateClient">True, if a client should be generated, otherwise False</param>
        /// <returns></returns>
        CodeCompileUnit GenerateInterfaceUnit(Feature feature, string ns, bool generateServer, bool generateClient);
    }
}