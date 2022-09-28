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
    internal class IdentifierValidation : IValidationCreator
    {
        public IEnumerable<ValidationSet> CreateValidation( CodePropertyReferenceExpression property, DataTypeType dataType, Constraints constraints, CodeTypeDeclaration context )
        {
            if(constraints.FullyQualifiedIdentifierSpecified && dataType.Item is BasicType.String)
            {
                var actualValue = new CodePropertyReferenceExpression( property, "Value" );
                var check = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression( nameof( IdentifierConstraint ) ), nameof( IdentifierConstraint.IsValid ),
                    actualValue,
                    new CodeFieldReferenceExpression( new CodeTypeReferenceExpression( typeof( IdentifierType ) ), GetFieldName( constraints.FullyQualifiedIdentifier ) ) );

                yield return new ValidationSet( new CodeBinaryOperatorExpression( check, CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression( false ) ),
                    actualValue.Format( $"{property.PropertyName} '{{0}}' is not a valid {constraints.FullyQualifiedIdentifier}" ) );
            }
        }

        private string GetFieldName( ConstraintsFullyQualifiedIdentifier fullyQualifiedIdentifier )
        {
            switch(fullyQualifiedIdentifier)
            {
                case ConstraintsFullyQualifiedIdentifier.FeatureIdentifier:
                    return nameof( IdentifierType.FeatureIdentifier );
                case ConstraintsFullyQualifiedIdentifier.CommandIdentifier:
                    return nameof( IdentifierType.CommandIdentifier );
                case ConstraintsFullyQualifiedIdentifier.CommandParameterIdentifier:
                    return nameof( IdentifierType.CommandParameterIdentifier );
                case ConstraintsFullyQualifiedIdentifier.CommandResponseIdentifier:
                    return nameof( IdentifierType.CommandResponseIdentifier );
                case ConstraintsFullyQualifiedIdentifier.IntermediateCommandResponseIdentifier:
                    return nameof( IdentifierType.IntermediateResponseIdentifier );
                case ConstraintsFullyQualifiedIdentifier.DefinedExecutionErrorIdentifier:
                    return nameof( IdentifierType.DefinedExecutionErrorIdentifier );
                case ConstraintsFullyQualifiedIdentifier.PropertyIdentifier:
                    return nameof( IdentifierType.PropertyIdentifier );
                case ConstraintsFullyQualifiedIdentifier.TypeIdentifier:
                    return nameof( IdentifierType.TypeIdentifier );
                case ConstraintsFullyQualifiedIdentifier.MetadataIdentifier:
                    return nameof( IdentifierType.MetadataIdentifier );
                default:
                    throw new NotSupportedException( $"Unknown identifier type {fullyQualifiedIdentifier}" );
            }
        }
    }
}
