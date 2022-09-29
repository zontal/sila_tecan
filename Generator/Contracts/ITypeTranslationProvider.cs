using System;
using System.CodeDom;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Denotes a component that manages translation of types
    /// </summary>
    public interface ITypeTranslationProvider
    {
        /// <summary>
        /// Tries to translate the given interface type to a SiLA2 type
        /// </summary>
        /// <param name="interfaceType">The .NET interface type</param>
        /// <param name="origin">An identifier of the type origin</param>
        /// <param name="silaType">The SiLA2 type</param>
        /// <returns>True, if the translation was successful, otherwise False</returns>
        bool TryTranslate( Type interfaceType, string origin, out DataTypeType silaType );

        /// <summary>
        /// Generates data transfer types for the given interface type
        /// </summary>
        /// <param name="interfaceType">The .NET interface type</param>
        /// <param name="origin">An identifier for the type origin</param>
        /// <param name="typeAction">A callback that should be executed when types are found</param>
        void TraverseTypes( Type interfaceType, string origin, Action<Type> typeAction );

        /// <summary>
        /// Tries to translate the given SiLA2 type to a .NET type translation info
        /// </summary>
        /// <param name="silaType">The SiLA2 type definition</param>
        /// <param name="suggestedName">The suggested name for anonymous types</param>
        /// <param name="translationInfo">An object describing the translation of this type</param>
        /// <param name="constraintHandler">A callback that is executed when the translation encounters a constraint</param>
        /// <param name="structHandler">A callback that is executed when the translation encounters an anonymous structure</param>
        /// <returns>True, if the translation information could be obtained, otherwise False</returns>
        bool TryTranslate( DataTypeType silaType, string suggestedName, out ITypeTranslationInfo translationInfo, Action<Constraints> constraintHandler = null,
            Action<string, StructureType> structHandler = null );

        /// <summary>
        /// Gets a reference to a data transfer object reference
        /// </summary>
        /// <param name="type">The SiLA2 type that should be translated</param>
        /// <param name="suggestedName">The suggested name for anonymous structures</param>
        /// <param name="structHandler">A callback for anonymous structures</param>
        /// <returns>A reference to the data transfer type for the given SiLA2 type</returns>
        CodeTypeReference GetDtoTypeReference( DataTypeType type, string suggestedName, Action<string, StructureType> structHandler );

        /// <summary>
        /// Generates a code fragment to encapsulate the given expression into a data transfer object
        /// </summary>
        /// <param name="argument">The expression to encapsulate</param>
        /// <param name="dataType">The data type of the expression</param>
        /// <param name="binaryStoreArgument">A reference to the binary storage parameter</param>
        /// <param name="suggestedName">The suggested name for anonymous structures</param>
        /// <returns></returns>
        CodeExpression EncapsulateAsDto( CodeExpression argument, DataTypeType dataType, CodeExpression binaryStoreArgument, string suggestedName );

        /// <summary>
        /// Extracts the default type reference for the given SiLA2 type
        /// </summary>
        /// <param name="dataType">The SiLA2 type</param>
        /// <param name="suggestedName">The suggested name of an anonymous SiLA2 type</param>
        /// <param name="constraintHandler">A callback if constraints are detected or null</param>
        /// <param name="registerStructure">A callback if anonymous structures are identified</param>
        /// <returns>The CodeDOM type reference of the SiLA2 type definition</returns>
        CodeTypeReference ExtractType( DataTypeType dataType, string suggestedName, Action<Constraints> constraintHandler = null,
            Action<string, StructureType> registerStructure = null );

        /// <summary>
        /// Generates code to extract the given expression from a data transfer object into the interface object
        /// </summary>
        /// <param name="expression">The expression that should be extracted</param>
        /// <param name="dataType">The SiLA2 type of the expression</param>
        /// <param name="binaryStorageArgument">The argument to use as binary store</param>
        /// <param name="targetType">An overridden target type or null</param>
        /// <returns>A CodeDOM expression that represents the extracted value</returns>
        CodeExpression Extract( CodeExpression expression, DataTypeType dataType, CodeExpression binaryStorageArgument, CodeTypeReference targetType );

        /// <summary>
        /// Generates code to extract the given expression from a data transfer object into the interface object
        /// </summary>
        /// <param name="expression">The expression that should be extracted</param>
        /// <param name="dataType">The SiLA2 type of the expression</param>
        /// <param name="binaryStorageArgument">The argument to use as binary store</param>
        /// <param name="parameterName">The name of the parameter for which the expression should be extracted</param>
        /// <param name="targetType">An overridden target type or null</param>
        /// <returns>A CodeDOM expression that represents the extracted value</returns>
        CodeExpression Extract( CodeExpression expression, DataTypeType dataType, CodeExpression binaryStorageArgument, string parameterName, CodeTypeReference targetType );
    }
}
