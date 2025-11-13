using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace Tecan.Sila2.Interop.Server.BasicDataTypesTest
{
    [Export( typeof( IBasicDataTypesTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class BasicDataTypesTest : IBasicDataTypesTest
    {
        public string StringValue => "SiLA2_Test_String_Value";

        public long IntegerValue => 5124;

        public double RealValue => 3.1415926;

        public bool BooleanValue => true;

        public DateTimeOffset DateValue => new DateTimeOffset( 2022, 8, 5, 0, 0, 0, TimeSpan.FromHours( 2 ) );

        public TimeSpan TimeValue => new TimeSpan( 0, 12, 34, 56, 789 );

        public DateTimeOffset TimestampValue => new DateTimeOffset( 2022, 8, 5, 12, 34, 56, 789, TimeSpan.FromHours( 2 ) );

        [return: SilaIdentifier( "ReceivedValue" )]
        public bool EchoBooleanValue( bool booleanValue )
        {
            return booleanValue;
        }

        [return: SilaIdentifier( "ReceivedValue" )]
        public DateTimeOffset EchoDateValue( DateTimeOffset dateValue )
        {
            return dateValue;
        }

        [return: SilaIdentifier( "ReceivedValue" )]
        public long EchoIntegerValue( long integerValue )
        {
            return integerValue;
        }

        [return: SilaIdentifier( "ReceivedValue" )]
        public double EchoRealValue( double realValue )
        {
            return realValue;
        }

        [return: SilaIdentifier( "ReceivedValue" )]
        public string EchoStringValue( string stringValue )
        {
            return stringValue;
        }

        [return: SilaIdentifier( "ReceivedValue" )]
        public DateTimeOffset EchoTimestampValue( DateTimeOffset timestampValue )
        {
            return timestampValue;
        }

        [return: SilaIdentifier( "ReceivedValue" )]
        public TimeSpan EchoTimeValue( TimeSpan timeValue )
        {
            return timeValue;
        }
    }
}
