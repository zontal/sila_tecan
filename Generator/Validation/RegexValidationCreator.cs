using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

namespace Tecan.Sila2.Generator.Validation
{
    [Export( typeof( IValidationCreator ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class RegexValidationCreator : IValidationCreator
    {
        public IEnumerable<ValidationSet> CreateValidation( CodePropertyReferenceExpression property, DataTypeType dataType, Constraints constraints, CodeTypeDeclaration context )
        {
            if(constraints.Pattern != null && dataType.Item is BasicType.String)
            {
                var actualValue = new CodePropertyReferenceExpression( property, "Value" );
                var check = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression( nameof( RegexConstraint ) ), nameof( RegexConstraint.IsMatch ),
                    actualValue,
                    new CodePrimitiveExpression( constraints.Pattern ) );

                yield return new ValidationSet( new CodeBinaryOperatorExpression( check, CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression( false ) ),
                    actualValue.Format( $"{property.PropertyName} '{{0}}' does not match pattern '{constraints.Pattern}'" ) );
            }
        }
    }
}
