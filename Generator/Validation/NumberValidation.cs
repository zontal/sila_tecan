using Common.Logging;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

namespace Tecan.Sila2.Generator.Validation
{
    [Export( typeof( IValidationCreator ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class NumberValidation : IValidationCreator
    {
        private static readonly ILog _log = LogManager.GetLogger( typeof( NumberValidation ) );

        public IEnumerable<ValidationSet> CreateValidation( CodePropertyReferenceExpression property, DataTypeType dataType, Constraints constraints, CodeTypeDeclaration context )
        {
            if(dataType.Item is not BasicType basicType)
            {
                return Enumerable.Empty<ValidationSet>();
            }

            var result = new List<ValidationSet>();
            TryAddValidationSet( property, CodeBinaryOperatorType.LessThan, basicType, constraints.MinimalInclusive, result );
            TryAddValidationSet( property, CodeBinaryOperatorType.LessThanOrEqual, basicType, constraints.MinimalExclusive, result );
            TryAddValidationSet( property, CodeBinaryOperatorType.GreaterThan, basicType, constraints.MaximalInclusive, result );
            TryAddValidationSet( property, CodeBinaryOperatorType.GreaterThanOrEqual, basicType, constraints.MaximalExclusive, result );
            return result;
        }

        private void TryAddValidationSet( CodePropertyReferenceExpression property, CodeBinaryOperatorType operatorType, BasicType type, string challenge, List<ValidationSet> validationSets )
        {
            if(string.IsNullOrEmpty( challenge ))
            {
                return;
            }
            try
            {
                var actualValue = CreateActualValue( property, type );
                var check = new CodeBinaryOperatorExpression( actualValue, operatorType, ParseChallenge( challenge, type ) );
                validationSets.Add( new ValidationSet( check, actualValue.Format( $"{property.PropertyName} should have been {GetNeededComparison( operatorType )} {challenge} but was '{{0}}'" ) ) );
            }
            catch(Exception ex)
            {
                _log.Warn( $"Failed to generate request validation for challenge {challenge} as a {type}. Please check the validity manually.", ex );
            }
        }

        private CodeExpression CreateActualValue( CodePropertyReferenceExpression property, BasicType type )
        {
            switch(type)
            {
                case BasicType.String:
                case BasicType.Integer:
                case BasicType.Real:
                case BasicType.Boolean:
                    return new CodePropertyReferenceExpression( property, "Value" );
                case BasicType.Date:
                case BasicType.Time:
                case BasicType.Timestamp:
                    return new CodeMethodInvokeExpression( property, nameof( ISilaTransferObject<object>.Extract ), new CodePrimitiveExpression() );
                default:
                    throw new ArgumentOutOfRangeException( nameof( type ) );
            }
        }

        private string GetNeededComparison( CodeBinaryOperatorType operatorType )
        {
            switch(operatorType)
            {
                case CodeBinaryOperatorType.LessThan:
                    return "at least";
                case CodeBinaryOperatorType.LessThanOrEqual:
                    return "at least more than";
                case CodeBinaryOperatorType.GreaterThan:
                    return "at most";
                case CodeBinaryOperatorType.GreaterThanOrEqual:
                    return "at most less than";
                default:
                    throw new ArgumentOutOfRangeException( nameof( operatorType ) );
            }
        }

        private CodeExpression ParseChallenge( string input, BasicType type )
        {
            switch(type)
            {
                case BasicType.Integer:
                    try
                    {
                        return new CodePrimitiveExpression( long.Parse( input, CultureInfo.InvariantCulture ) );
                    }
                    catch(FormatException)
                    {
                        return new CodePrimitiveExpression( (long)double.Parse( input, CultureInfo.InvariantCulture ) );
                    }
                case BasicType.Real:
                    return new CodePrimitiveExpression( double.Parse( input, CultureInfo.InvariantCulture ) );
                case BasicType.Date:
                    var date = DateTimeOffset.Parse( input, CultureInfo.InvariantCulture );
                    return new CodeObjectCreateExpression( typeof( DateTimeOffset ),
                        new CodePrimitiveExpression( date.Year ),
                        new CodePrimitiveExpression( date.Month ),
                        new CodePrimitiveExpression( date.Day ),
                        new CodePrimitiveExpression( 0 ),
                        new CodePrimitiveExpression( 0 ),
                        new CodePrimitiveExpression( 0 ),
                        new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( typeof( TimeSpan ) ),
                            nameof( TimeSpan.FromMinutes ),
                            new CodePrimitiveExpression( date.Offset.TotalMinutes ) ) );
                case BasicType.Time:
                    try
                    {
                        var time = TimeSpan.Parse( input, CultureInfo.InvariantCulture );
                        return new CodeObjectCreateExpression( typeof( TimeSpan ),
                            new CodePrimitiveExpression( time.Hours + 24 * ((int)time.TotalDays) ),
                            new CodePrimitiveExpression( time.Minutes ),
                            new CodePrimitiveExpression( time.Seconds ) );
                    }
                    catch(FormatException)
                    {
                        var time = DateTimeOffset.Parse( input, CultureInfo.InvariantCulture ).ToUniversalTime();
                        return new CodeObjectCreateExpression( typeof( TimeSpan ),
                            new CodePrimitiveExpression( time.Hour ),
                            new CodePrimitiveExpression( time.Minute ),
                            new CodePrimitiveExpression( time.Second ) );
                    }
                case BasicType.Timestamp:
                    var timeStamp = DateTimeOffset.Parse( input, CultureInfo.InvariantCulture );
                    return new CodeObjectCreateExpression( typeof( DateTimeOffset ),
                        new CodePrimitiveExpression( timeStamp.Year ),
                        new CodePrimitiveExpression( timeStamp.Month ),
                        new CodePrimitiveExpression( timeStamp.Day ),
                        new CodePrimitiveExpression( timeStamp.Hour ),
                        new CodePrimitiveExpression( timeStamp.Minute ),
                        new CodePrimitiveExpression( timeStamp.Second ),
                        new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( typeof( TimeSpan ) ),
                            nameof( TimeSpan.FromMinutes ),
                            new CodePrimitiveExpression( timeStamp.Offset.TotalMinutes ) ) );
                default:
                    throw new NotSupportedException( $"Numeric constraints are not supported for type {type}" );
            }
        }
    }
}
