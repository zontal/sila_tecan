using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Tecan.Sila2.DynamicClient;

namespace Tecan.Sila2.Interop.Server.AnyTypeTest
{
    [Export( typeof( IAnyTypeTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class AnyTypeTest : IAnyTypeTest
    {
        public DynamicObjectProperty AnyTypeStringValue => new DynamicObjectProperty( "SiLA_Any_type_of_String_type" );

        public DynamicObjectProperty AnyTypeIntegerValue => new DynamicObjectProperty( 5124 );

        public DynamicObjectProperty AnyTypeRealValue => new DynamicObjectProperty( 3.1415926 );

        public DynamicObjectProperty AnyTypeBooleanValue => new DynamicObjectProperty( true );

        public DynamicObjectProperty AnyTypeBinaryValue => new DynamicObjectProperty( Encoding.ASCII.GetBytes( "SiLA_Any_type_of_Binary_type" ) );

        public DynamicObjectProperty AnyTypeDateValue => new DynamicObjectProperty( new DateTimeOffset( 2022, 8, 5, 0, 0, 0, TimeSpan.FromHours( 2 ) ), BasicType.Date );

        public DynamicObjectProperty AnyTypeTimeValue => new DynamicObjectProperty( new DateTimeOffset( 2022, 8, 5, 12, 34, 56, 789, TimeSpan.FromHours( 2 ) ), BasicType.Time );

        public DynamicObjectProperty AnyTypeTimestampValue => new DynamicObjectProperty( new DateTimeOffset( 2022, 8, 5, 12, 34, 56, 789, TimeSpan.FromHours( 2 ) ) );

        public DynamicObjectProperty AnyTypeListValue => new DynamicObjectProperty( new List<string> { "SiLA 2", "Any", "Type", "String", "List" },
            new DataTypeType { Item = new ListType { DataType = new DataTypeType { Item = BasicType.String } } } );

        public DynamicObjectProperty AnyTypeStructureValue => CreateDynamicStructValue();

        private static DynamicObjectProperty CreateDynamicStructValue()
        {
            var stringProperty = CreateProperty( "StringTypeValue", "String Type Value", "A string value.", BasicType.String, "A String value" );
            var integerProperty = CreateProperty( "IntegerTypeValue", "Integer Type Value", "An integer value.", BasicType.Integer, 83737665L );
            var dateProperty = CreateProperty( "DateTypeValue", "Date Type Value", "A date value.", BasicType.Date, new DateTimeOffset( 2022, 8, 5, 0, 0, 0, TimeSpan.FromHours( 2 ) ) );


            return new DynamicObjectProperty(
                new DynamicObject
                {
                    Elements =
                    {
                        stringProperty,
                        integerProperty,
                        dateProperty
                    }
                } );
        }

        private static DynamicObjectProperty CreateProperty( string name, string displayName, string description, BasicType basicType, object value )
        {
            return new DynamicObjectProperty( name, displayName, description, new DataTypeType { Item = basicType } )
            {
                Value = value
            };
        }

        [return: InlineStruct]
        public SetAnyTypeValueResponse SetAnyTypeValue( DynamicObjectProperty anyTypeValue )
        {
            var typeString = DataTypeSerializer.SaveToString( anyTypeValue.Type );
            return new SetAnyTypeValueResponse( typeString, anyTypeValue );
        }
    }
}
