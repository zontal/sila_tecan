using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Denotes a component that can render request validation
    /// </summary>
    public interface IValidationCreator
    {
        /// <summary>
        /// Creates a collection of validations for the given value
        /// </summary>
        /// <param name="property">An expression of the value to be validated</param>
        /// <param name="dataType">The SiLA2 data type of the expression</param>
        /// <param name="constraints">The constraints that are to be applied</param>
        /// <param name="context">The context class in which the validation is used</param>
        /// <returns>A collection of validation statements</returns>
        IEnumerable<ValidationSet> CreateValidation( CodePropertyReferenceExpression property, DataTypeType dataType, Constraints constraints, CodeTypeDeclaration context );
    }
}
