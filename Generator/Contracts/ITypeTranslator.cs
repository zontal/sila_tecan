using System;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Denotes a component that can translate between .NET types and SiLA2 types
    /// </summary>
    public interface ITypeTranslator
    {
        /// <summary>
        /// Tries to translate the given interface type to a SiLA2 type
        /// </summary>
        /// <param name="translationProvider">The provider in the context of which the translation is done</param>
        /// <param name="interfaceType">The .NET interface type</param>
        /// <param name="origin">An identifier of the type origin</param>
        /// <param name="silaType">The SiLA2 type</param>
        /// <returns>True, if the translation was successful, otherwise False</returns>
        bool TryTranslate( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, out DataTypeType silaType );

        /// <summary>
        /// Generates data transfer types for the given interface type
        /// </summary>
        /// <param name="translationProvider">The provider in the context of which the translation is done</param>
        /// <param name="interfaceType">The .NET interface type</param>
        /// <param name="origin">An identifier for the type origin</param>
        /// <param name="typeAction">A callback that should be executed when types are found</param>
        bool TraverseTypes( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, Action<Type> typeAction );

        /// <summary>
        /// Tries to translate the given SiLA2 type to a .NET type and transfer DTO
        /// </summary>
        /// <param name="translationProvider">The provider in the context of which the translation is done</param>
        /// <param name="silaType">The SiLA2 type</param>
        /// <param name="suggestedName">The suggested name for anonymous structs</param>
        /// <param name="translationInfo">An object that defines how the SiLA2 type should be translated</param>
        /// <param name="constraintHandler">A callback that should be executed when the translation provider finds constraints</param>
        /// <param name="structHandler">A callback that should be executed when the translation provider encounters an anonymous structure type</param>
        /// <returns>True, if the translation was successful, otherwise False</returns>
        bool TryTranslate( ITypeTranslationProvider translationProvider, DataTypeType silaType, string suggestedName, out ITypeTranslationInfo translationInfo, Action<Constraints> constraintHandler = null, Action<string, StructureType> structHandler = null );

        /// <summary>
        /// Gets the priority of the given translator 
        /// </summary>
        int Priority
        {
            get;
        }
    }
}
