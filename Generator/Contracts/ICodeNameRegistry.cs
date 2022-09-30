using System;
using System.Reflection;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Registers deviations from convention-based naming
    /// </summary>
    public interface ICodeNameRegistry
    {
        /// <summary>
        /// Registers that the identifier with the given name has been renamed
        /// </summary>
        /// <param name="memberName">The name of the member</param>
        /// <param name="identifier">The new identifier</param>
        void RegisterRename( string memberName, string identifier );

        /// <summary>
        /// Registers that for the given type, the given construction method should be used
        /// </summary>
        /// <param name="type">The type that should be constructed</param>
        /// <param name="constructorMethod">The construction method</param>
        void RegisterConstructionMethod( Type type, MethodInfo constructorMethod );

        /// <summary>
        /// Registers that the feature element with the given identifier uses a different type than expected
        /// </summary>
        /// <param name="identifier">The identifier of the feature element</param>
        /// <param name="type">The deviating type</param>
        void RegisterDifferentType( string identifier, Type type );

        /// <summary>
        /// Determines whether the element with the given identifier has been registered with a different name
        /// </summary>
        /// <param name="identifier">The identifier of the feature element</param>
        /// <returns>True, if a different type has been registered, otherwise False</returns>
        bool IsDifferentTypeRegistered( string identifier );

        /// <summary>
        /// Registers that the command with the given identifier has a non-standard method
        /// </summary>
        /// <param name="commandIdentifier">The identifier of the command</param>
        /// <param name="method">The original command method</param>
        void RegisterMethod( string commandIdentifier, MethodInfo method );
    }
}