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
    internal class LengthValidation : IValidationCreator
    {
        public IEnumerable<ValidationSet> CreateValidation( CodePropertyReferenceExpression property, DataTypeType dataType, Constraints constraints, CodeTypeDeclaration context )
        {
            CodeExpression compare;
            if(dataType.Item is BasicType.String)
            {
                compare = new CodePropertyReferenceExpression( new CodePropertyReferenceExpression( property, "Value" ), "Length" );
            }
            else if(dataType.Item is ListType)
            {
                compare = new CodePropertyReferenceExpression( property, nameof( List<object>.Count ) );
            }
            else
            {
                yield break;
            }

            if(!string.IsNullOrEmpty( constraints.MaximalLength ))
            {
                var maxLength = int.Parse( constraints.MaximalLength );
                var check = new CodeBinaryOperatorExpression( compare, CodeBinaryOperatorType.GreaterThan, new CodePrimitiveExpression( maxLength ) );

                yield return new ValidationSet( check, compare.Format( $"{property.PropertyName}  has length {{0}} and is longer than allowed length {maxLength}" ) );
            }

            if(!string.IsNullOrEmpty( constraints.MinimalLength ))
            {
                var minLength = int.Parse( constraints.MinimalLength );
                var check = new CodeBinaryOperatorExpression( compare, CodeBinaryOperatorType.LessThan, new CodePrimitiveExpression( minLength ) );

                yield return new ValidationSet( check, compare.Format( $"{property.PropertyName} has length {{0}} and is shorter than required length {minLength}" ) );
            }

            if(!string.IsNullOrEmpty( constraints.Length ))
            {
                var length = int.Parse( constraints.Length );
                var check = new CodeBinaryOperatorExpression( compare, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression( length ) );

                yield return new ValidationSet( check, compare.Format( $"{property.PropertyName} has length {{0}} but should have been {length}" ) );
            }
        }
    }
}
