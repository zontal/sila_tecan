using System.CodeDom;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Describes how to translate a given SiLA2 type to a .NET type
    /// </summary>
    public interface ITypeTranslationInfo
    {
        /// <summary>
        /// Gets a reference to the interface (or business) type
        /// </summary>
        CodeTypeReference InterfaceType
        {
            get;
        }

        /// <summary>
        /// Gets a reference to the data transfer type
        /// </summary>
        CodeTypeReference DataTransferType
        {
            get;
        }

        /// <summary>
        /// Generates a code expression that encapsulates the given business object
        /// </summary>
        /// <param name="expression">The expression to be encapsulated</param>
        /// <param name="binaryStorageArgument">An expression for the binary storage argument</param>
        /// <returns>A code expression that encapsulates the given expression</returns>
        CodeExpression Encapsulate( CodeExpression expression, CodeExpression binaryStorageArgument );

        /// <summary>
        /// Generates a code expression that extracts the business object from the given data transfer expression
        /// </summary>
        /// <param name="expression">The expression with the data transfer object</param>
        /// <param name="targetType">The target type reference</param>
        /// <param name="binaryStorageArgument">An expression for the binary storage argument</param>
        /// <returns>A code expression that extracts the given code expression</returns>
        CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument );

        /// <summary>
        /// Generates a code expression that extracts the business object from the given data transfer expression
        /// </summary>
        /// <param name="expression">The expression with the data transfer object</param>
        /// <param name="targetType">The target type reference</param>
        /// <param name="binaryStorageArgument">An expression for the binary storage argument</param>
        /// <param name="parameterName">The parameter for which the element should be extracted</param>
        /// <returns>A code expression that extracts the given code expression</returns>
        CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument, string parameterName );
    }
}
