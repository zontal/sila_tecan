using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Tecan.Sila2.DynamicClient;

namespace Tecan.Sila2.Interop.Server.ParameterConstraintsTest
{
    [Export( typeof( IParameterConstraintsTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class ParameterConstraintsTest : IParameterConstraintsTest
    {
        private readonly Regex _patternRegex = new Regex( "[0-9]{2}/[0-9]{2}/[0-9]{2}/[0-9]{4}", RegexOptions.Compiled );

        // normally, the code generator would generate an enum, but not if the options contain whitespaces
        private static string[] _allowedStrings = new string[]
        {
            "First option",
            "Second option",
            "Third option"
        };

        #region String constraints

        public void CheckStringConstraintLength( string constrainedParameter )
        {
            if(constrainedParameter.Length != 10)
            {
                throw new ArgumentException( "String length must be 10", nameof( constrainedParameter ) );
            }
        }

        public void CheckStringConstraintMinimalLength( string constrainedParameter )
        {
            if(constrainedParameter.Length < 5)
            {
                throw new ArgumentException( "String must be at least 5", nameof( constrainedParameter ) );
            }
        }

        public void CheckStringConstraintMaximalLength( [MaximalLength( 20 )] string constrainedParameter )
        {
            if(constrainedParameter.Length > 20)
            {
                throw new ArgumentException( "String length must be at most 20", nameof( constrainedParameter ) );
            }
        }

        public void CheckStringConstraintMinMaxLength( [MaximalLength( 20 )] string constrainedParameter )
        {
            if(constrainedParameter.Length < 5 || constrainedParameter.Length > 20)
            {
                throw new ArgumentException( "String length must be between 5 and 20", nameof( constrainedParameter ) );
            }
        }

        public void CheckStringConstraintSet( string constrainedParameter )
        {
            if(!_allowedStrings.Contains( constrainedParameter ))
            {
                throw new ArgumentException( "String is not allowed", nameof( constrainedParameter ) );
            }
        }

        public void CheckStringConstraintPattern( [PatternConstraint( "[0-9]{2}/[0-9]{2}/[0-9]{4}" )] string constrainedParameter )
        {
            if(!_patternRegex.IsMatch( constrainedParameter ))
            {
                throw new ArgumentException( "String must be according to pattern", nameof( constrainedParameter ) );
            }
        }

        public void CheckStringConstraintContentType( [ContentType( "application", "xml" )] string constrainedParameter )
        {
            var reader = XmlReader.Create( new StringReader( constrainedParameter ) );
            try
            {
                while(reader.Read()) { }
            }
            catch(XmlException)
            {
                throw new ArgumentException( "Argument is not proper XML", nameof( constrainedParameter ) );
            }
        }

        public void CheckStringConstraintFullyQualifiedIdentifier( [SilaIdentifierType( IdentifierType.FeatureIdentifier )] string featureIdentifier, [SilaIdentifierType( IdentifierType.CommandIdentifier )] string commandIdentifier, [SilaIdentifierType( IdentifierType.CommandParameterIdentifier )] string commandParameterIdentifier, [SilaIdentifierType( IdentifierType.CommandResponseIdentifier )] string commandResponseIdentifier, [SilaIdentifierType( IdentifierType.IntermediateResponseIdentifier )] string intermediateCommandResponseIdentifier, [SilaIdentifierType( IdentifierType.DefinedExecutionErrorIdentifier )] string executionErrorIdentifier, [SilaIdentifierType( IdentifierType.PropertyIdentifier )] string propertyIdentifier, [SilaIdentifierType( IdentifierType.TypeIdentifier )] string customDataTypeIdentifier, [SilaIdentifierType( IdentifierType.MetadataIdentifier )] string metadataIdentifier )
        {
            IdentifierConstraint.CheckParameter( featureIdentifier, IdentifierType.FeatureIdentifier, nameof( featureIdentifier ) );
            IdentifierConstraint.CheckParameter( commandIdentifier, IdentifierType.CommandIdentifier, nameof( commandIdentifier ) );
            IdentifierConstraint.CheckParameter( commandParameterIdentifier, IdentifierType.CommandParameterIdentifier, nameof( commandParameterIdentifier ) );
            IdentifierConstraint.CheckParameter( commandResponseIdentifier, IdentifierType.CommandResponseIdentifier, nameof( commandResponseIdentifier ) );
            IdentifierConstraint.CheckParameter( intermediateCommandResponseIdentifier, IdentifierType.IntermediateResponseIdentifier, nameof( intermediateCommandResponseIdentifier ) );
            IdentifierConstraint.CheckParameter( executionErrorIdentifier, IdentifierType.DefinedExecutionErrorIdentifier, nameof( executionErrorIdentifier ) );
            IdentifierConstraint.CheckParameter( propertyIdentifier, IdentifierType.PropertyIdentifier, nameof( propertyIdentifier ) );
            IdentifierConstraint.CheckParameter( customDataTypeIdentifier, IdentifierType.TypeIdentifier, nameof( customDataTypeIdentifier ) );
            IdentifierConstraint.CheckParameter( metadataIdentifier, IdentifierType.MetadataIdentifier, nameof( metadataIdentifier ) );
        }

        public void CheckStringConstraintSchema( [Schema( "https://gitlab.com/SiLA2/sila_base/-/blob/master/schema/FeatureDefinition.xsd", SchemaType.Xml )] string constrainedParameter )
        {
            try
            {
                FeatureSerializer.LoadFromXml( constrainedParameter );
            }
            catch(Exception)
            {
                throw new ArgumentException( "Parameter is not a valid Feature.", nameof( constrainedParameter ) );
            }
        }
        #endregion
        #region Integer constraints

        public void CheckIntegerConstraintSet( long constrainedParameter )
        {
            if(constrainedParameter != 1 && constrainedParameter != 2 && constrainedParameter != 3)
            {
                throw new ArgumentException( "Value must be 1, 2 or 3", nameof( constrainedParameter ) );
            }
        }

        public void CheckScientificallyNotatedIntegerLimit( [MaximalExclusive( 12300000 ), MinimalInclusive( -1000 )] long constrainedParameter )
        {
            if(constrainedParameter > 12300000 || constrainedParameter < -1000)
            {
                throw new ArgumentException( "Value must be between 0 and 10", nameof( constrainedParameter ) );
            }
        }

        public void CheckIntegerConstraintMaximalExclusive( [MaximalExclusive( 11 )] long constrainedParameter )
        {
            if(constrainedParameter >= 11)
            {
                throw new ArgumentException( "Value must be less than 11", nameof( constrainedParameter ) );
            }
        }

        public void CheckIntegerConstraintMaximalInclusive( [MaximalInclusive( 10 )] long constrainedParameter )
        {
            if(constrainedParameter > 10)
            {
                throw new ArgumentException( "Value must be at most 10", nameof( constrainedParameter ) );
            }
        }

        public void CheckIntegerConstraintMinimalExclusive( [MinimalExclusive( -1 )] long constrainedParameter )
        {
            if(constrainedParameter <= -1)
            {
                throw new ArgumentException( "Value must be greater than -1", nameof( constrainedParameter ) );
            }
        }

        public void CheckIntegerConstraintMinimalInclusive( [MinimalInclusive( 0 )] long constrainedParameter )
        {
            if(constrainedParameter < 0)
            {
                throw new ArgumentException( "Value must be at least 0", nameof( constrainedParameter ) );
            }
        }

        public void CheckIntegerConstraintMinMax( [MaximalInclusive( 10 ), MinimalExclusive( -1 )] long constrainedParameter )
        {
            if(constrainedParameter <= -1 || constrainedParameter > 10)
            {
                throw new ArgumentException( "Value must be between -1 and 10", nameof( constrainedParameter ) );
            }
        }

        public void CheckIntegerConstraintUnit( [Unit( "mL", Meter = 3, Factor = 1E-06 )] long constrainedParameter )
        {
        }
        #endregion
        #region Real constraints
        public void CheckRealConstraintSet( double constrainedParameter )
        {
            if(constrainedParameter != 1.11 && constrainedParameter != 2.22 && constrainedParameter != 3.33)
            {
                throw new ArgumentException( "Value must be 1.11, 2.22 or 3.33", nameof( constrainedParameter ) );
            }
        }

        public void CheckScientificallyNotatedRealLimit( [MaximalExclusive( 123.456 ), MinimalInclusive( 0.00123 )] double constrainedParameter )
        {
            if(constrainedParameter < 0.123 || constrainedParameter >= 123.456)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckRealConstraintMaximalExclusive( [MaximalExclusive( 10.5 )] double constrainedParameter )
        {
            if(constrainedParameter >= 10.5)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckRealConstraintMaximalInclusive( [MaximalInclusive( 10 )] double constrainedParameter )
        {
            if(constrainedParameter > 10)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckRealConstraintMinimalExclusive( [MinimalExclusive( 0 )] double constrainedParameter )
        {
            if(constrainedParameter <= 0)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckRealConstraintMinimalInclusive( [MinimalInclusive( -1.5 )] double constrainedParameter )
        {
            if(constrainedParameter < -1.5)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckRealConstraintMinMax( [MaximalInclusive( 11.111 ), MinimalExclusive( 0.333 )] double constrainedParameter )
        {
            if(constrainedParameter <= 0.333 || constrainedParameter > 11.111)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckRealConstraintUnit( [Unit( "°C", Kelvin = 1, Offset = 273.15 )] double constrainedParameter )
        {
        }
        #endregion
        #region Date constraints
        public void CheckDateConstraintSet( DateTimeOffset constrainedParameter )
        {
            if(constrainedParameter != new DateTime( 2022, 9, 10 )
                && constrainedParameter != new DateTimeOffset( 2022, 9, 11, 0, 0, 0, TimeSpan.FromMinutes( -15 ) )
                && constrainedParameter != new DateTimeOffset( 2022, 9, 12, 0, 0, 0, TimeSpan.FromMinutes( 690 ) ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckDateConstraintMaximalExclusive( [MaximalExclusiveDate( "2019-12-31+02:30" )] DateTimeOffset constrainedParameter )
        {
            if(constrainedParameter >= new DateTimeOffset( 2019, 12, 31, 0, 0, 0, TimeSpan.FromMinutes( 150 ) ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckDateConstraintMaximalInclusive( [MaximalInclusiveDate( "2025-12-31-01:30" )] DateTimeOffset constrainedParameter )
        {
            if(constrainedParameter > new DateTimeOffset( 2025, 12, 31, 0, 0, 0, TimeSpan.FromMinutes( -90 ) ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckDateConstraintMinimalExclusive( [MinimalExclusiveDate( "2019-01-01-01:00" )] DateTimeOffset constrainedParameter )
        {
            if(constrainedParameter <= new DateTimeOffset( 2019, 1, 1, 0, 0, 0, TimeSpan.FromHours( -1 ) ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckDateConstraintMinimalInclusive( [MinimalInclusiveDate( "1789-07-14+02:00" )] DateTimeOffset constrainedParameter )
        {
            if(constrainedParameter < new DateTimeOffset( 1789, 7, 14, 0, 0, 0, TimeSpan.FromHours( 2 ) ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckDateConstraintMinMax( [MaximalExclusiveDate( "2076-06-10+02:00" ), MinimalInclusiveDate( "1976-06-10+02:00" )] DateTimeOffset constrainedParameter )
        {
            if(constrainedParameter < new DateTimeOffset( 1976, 6, 10, 0, 0, 0, TimeSpan.FromHours( 2 ) )
                || constrainedParameter >= new DateTimeOffset( 2076, 6, 10, 0, 0, 0, TimeSpan.FromHours( 2 ) ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }
        #endregion
        #region Time constraints
        public void CheckTimeConstraintSet( TimeSpan constrainedParameter )
        {
            if(constrainedParameter != new TimeSpan( 16, 59, 59 )
                && constrainedParameter != new TimeSpan( 19, 15, 0 )
                && constrainedParameter != new TimeSpan( 19, 59, 0 ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckTimeConstraintMaximalExclusive( [MaximalExclusiveDate( "12:00:00+00:15" )] TimeSpan constrainedParameter )
        {
            if(constrainedParameter >= new TimeSpan( 11, 45, 0 ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckTimeConstraintMaximalInclusive( [MaximalInclusiveDate( "18:59:59+02:00" )] TimeSpan constrainedParameter )
        {
            if(constrainedParameter > new TimeSpan( 16, 59, 59 ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckTimeConstraintMinimalExclusive( [MinimalExclusiveDate( "01:02:03+04:00" )] TimeSpan constrainedParameter )
        {
            if(constrainedParameter <= new TimeSpan( -3, 2, 3 ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckTimeConstraintMinimalInclusive( [MinimalInclusiveDate( "08:00:00Z" )] TimeSpan constrainedParameter )
        {
            if(constrainedParameter < new TimeSpan( 8, 0, 0 ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckTimeConstraintMinMax( [MaximalInclusiveDate( "18:59:59+02:30" ), MinimalExclusiveDate( "08:00:00-02:30" )] TimeSpan constrainedParameter )
        {
            if(constrainedParameter > new TimeSpan( 16, 29, 59 )
                || constrainedParameter <= new TimeSpan( 10, 30, 0 ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }
        #endregion
        #region Timestamp constraints
        public void CheckTimestampConstraintSet( DateTimeOffset constrainedParameter )
        {
            if(constrainedParameter != new DateTime( 1969, 7, 16, 13, 32, 0 )
                && constrainedParameter != new DateTimeOffset( 1969, 7, 20, 20, 17, 40, TimeSpan.Zero )
                && constrainedParameter != new DateTimeOffset( 1969, 7, 24, 16, 50, 35, TimeSpan.Zero ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckTimestampConstraintMaximalExclusive( [MaximalExclusiveDate( "2022-09-07T16:00:00+02:00" )] DateTimeOffset constrainedParameter )
        {
            if(constrainedParameter >= new DateTimeOffset( 2022, 9, 7, 16, 0, 0, TimeSpan.FromHours( 2 ) ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckTimestampConstraintMaximalInclusive( [MaximalInclusiveDate( "2025-12-31T13:59:59-01:30" )] DateTimeOffset constrainedParameter )
        {
            if(constrainedParameter > new DateTimeOffset( 2025, 12, 31, 13, 59, 59, TimeSpan.FromMinutes( -90 ) ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckTimestampConstraintMinimalExclusive( [MinimalExclusiveDate( "2019-01-01T00:00:00-01:00" )] DateTimeOffset constrainedParameter )
        {
            if(constrainedParameter <= new DateTimeOffset( 2019, 1, 1, 0, 0, 0, TimeSpan.FromHours( -1 ) ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckTimestampConstraintMinimalInclusive( [MinimalInclusiveDate( "2022-09-07T13:48:59+02:00" )] DateTimeOffset constrainedParameter )
        {
            if(constrainedParameter < new DateTimeOffset( 2022, 9, 7, 13, 48, 59, TimeSpan.FromHours( 2 ) ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckTimestampConstraintMinMax( [MaximalExclusiveDate( "1972-12-11T19:54:57Z" ), MinimalInclusiveDate( "1969-07-20T20:17:40Z" )] DateTimeOffset constrainedParameter )
        {
            if(constrainedParameter < new DateTimeOffset( 1972, 12, 11, 19, 54, 57, TimeSpan.Zero )
                || constrainedParameter >= new DateTimeOffset( 1969, 7, 20, 20, 17, 40, TimeSpan.Zero ))
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }
        #endregion
        #region List constraints
        public void CheckListConstraintElementCount( ICollection<string> constrainedParameter )
        {
            if(constrainedParameter.Count != 5)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckListConstraintMinimalElementCount( ICollection<string> constrainedParameter )
        {
            if(constrainedParameter.Count < 3)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckListConstraintMaximalElementCount( ICollection<string> constrainedParameter )
        {
            if(constrainedParameter.Count > 5)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckListConstraintMinMaxElementCount( ICollection<string> constrainedParameter )
        {
            if(constrainedParameter.Count < 2 || constrainedParameter.Count > 5)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }
        #endregion
        #region Binary constraints
        public void CheckBinaryConstraintLength( Stream constrainedParameter )
        {
            if(constrainedParameter.Length != 10)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckBinaryConstraintMinimalLength( Stream constrainedParameter )
        {
            if(constrainedParameter.Length < 5)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckBinaryConstraintMaximalLength( [MaximalLength( 20 )] Stream constrainedParameter )
        {
            if(constrainedParameter.Length > 20)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckBinaryConstraintMinMaxLength( [MaximalLength( 20 )] Stream constrainedParameter )
        {
            if(constrainedParameter.Length < 5 || constrainedParameter.Length > 20)
            {
                throw new ArgumentOutOfRangeException( nameof( constrainedParameter ) );
            }
        }

        public void CheckBinaryConstraintContentType( [ContentType( "application", "xml" )] Stream constrainedParameter )
        {
            try
            {
                var reader = XmlReader.Create( constrainedParameter );
                while(reader.Read()) { }
            }
            catch(XmlException)
            {
                throw new ArgumentException( "Not valid XML", nameof( constrainedParameter ) );
            }
        }

        public void CheckBinaryConstraintSchema( [Schema( "https://gitlab.com/SiLA2/sila_base/-/blob/master/schema/FeatureDefinition.xsd", SchemaType.Xml )] Stream constrainedParameter )
        {
            try
            {
                FeatureSerializer.Load( constrainedParameter );
            }
            catch(Exception)
            {
                throw new ArgumentException( "Not a valid feature", nameof( constrainedParameter ) );
            }
        }
        #endregion
        #region AnyType constraints
        public void CheckAllowedTypesConstraint( DynamicObjectProperty constrainedParameter )
        {
            if(!(constrainedParameter.Value is long || constrainedParameter.Value is IList<long>))
            {
                throw new ArgumentException( "Parameter has the wrong type", nameof( constrainedParameter ) );
            }
        }

        public void CheckAllowedStructureTypeConstraints( DynamicObjectProperty parameter )
        {
            if(!(parameter.Value is DynamicObject obj && obj.Elements.Count == 1 && obj.Elements[0].Identifier == "Item" && obj.Elements[0].Value is long))
            {
                throw new ArgumentException( "Parameter has the wrong type", nameof( parameter ) );
            }
        }
        #endregion
    }
}
