using System.CodeDom;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Denotes a component that adds dependency injection registrations
    /// </summary>
    public interface IDependencyInjectionGenerator
    {
        /// <summary>
        /// Implements dependency injection for the given code declaration
        /// </summary>
        /// <param name="codeTypeDeclaration">The code declaration that should get dependency injection registrations</param>
        void AddDependencyInjectionRegistrations( CodeTypeDeclaration codeTypeDeclaration );
    }
}
