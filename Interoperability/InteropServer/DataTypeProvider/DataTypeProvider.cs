using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;

namespace Tecan.Sila2.Interop.Server.DataTypeProvider
{
    [Export( typeof( IDataTypeProvider ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class DataTypeProvider : IDataTypeProvider
    {
        public string StringValue => "SiLA2_Test_String_Value";

        public long IntegerValue => 42;

        public double RealValue => 3.1415926;

        public bool BooleanValue => true;

        public Stream BinaryValue
        {
            get
            {
                var ms = new MemoryStream();
                ms.Write( Encoding.UTF8.GetBytes( StringValue ) );
                ms.Position = 0;
                return ms;
            }
        }

        public DateTimeOffset DateValue => new DateTimeOffset( 2019, 8, 24, 0, 0, 0, default );

        public TimeSpan TimeValue => new TimeSpan( 12, 34, 56 );

        public DateTimeOffset TimeStampValue => new DateTimeOffset( 2019, 8, 24, 12, 34, 56, default );

        public ICollection<string> StringList { get; } = new List<string>()
        {
            "SiLA 2",
            "is",
            "great"
        };

        public ICollection<long> IntegerList { get; } = new List<long>()
        {
            1, 2, 3
        };

        public AllDatatypeStructure StructureValues => new AllDatatypeStructure( StringValue, IntegerValue, RealValue, BooleanValue, BinaryValue, DateValue, TimeValue, TimeStampValue );

        [return: SilaDisplayName( "Received Value" ), SilaIdentifier( "ReceivedBinaryValue" )]
        public Stream EchoBinaryValue( Stream binaryValue )
        {
            return binaryValue;
        }

        [return: SilaIdentifier( "ReceivedBooleanValue" )]
        public bool EchoBooleanValue( bool booleanValue )
        {
            return booleanValue;
        }

        [return: SilaIdentifier( "ReceivedDateValue" )]
        public DateTimeOffset EchoDateValue( DateTimeOffset dateValue )
        {
            return dateValue;
        }

        [return: SilaIdentifier( "ReceivedValues" )]
        public long EchoIntegerList( ICollection<long> integerList )
        {
            return integerList.Sum();
        }

        [return: SilaIdentifier( "ReceivedIntegerValue" )]
        public long EchoIntegerValue( long integerValue )
        {
            return integerValue;
        }

        [return: SilaIdentifier( "ReceivedValues" )]
        public string EchoMultipleMixedValues( string stringValue, long integerValue, double realValue, bool booleanValue, DateTimeOffset dateValue, TimeSpan timeValue, [SilaDisplayName( "TimeStamp Value" )] DateTimeOffset timeStampValue )
        {
            return $"{stringValue}, {integerValue}, {realValue}, {booleanValue}, {dateValue}, {timeValue}, {timeStampValue}";
        }

        [return: SilaDisplayName( "Received Value" ), SilaIdentifier( "ReceivedRealValue" )]
        public double EchoRealValue( double realValue )
        {
            return realValue;
        }

        [return: SilaIdentifier( "ReceivedValues" )]
        public ICollection<string> EchoStringList( ICollection<string> stringList )
        {
            return stringList;
        }

        [return: SilaIdentifier( "ReceivedStringValue" )]
        public string EchoStringValue( string stringValue )
        {
            return stringValue;
        }

        public void EchoStructureValues( AllDatatypeStructure receivedValues )
        {
        }

        [return: SilaIdentifier( "ReceivedValue" )]
        public DateTimeOffset EchoTimeStampValue( [SilaDisplayName( "TimeStamp Value" )] DateTimeOffset timeStampValue )
        {
            return timeStampValue;
        }

        [return: SilaDisplayName( "Received Value" ), SilaIdentifier( "ReceivedTimeValue" )]
        public TimeSpan EchoTimeValue( TimeSpan timeValue )
        {
            return timeValue;
        }

        [return: InlineStruct]
        public GetMultipleMixedValuesResponse GetMultipleMixedValues()
        {
            return new GetMultipleMixedValuesResponse(
                StringValue,
                IntegerValue,
                RealValue,
                BooleanValue,
                DateValue,
                TimeValue,
                TimeStampValue );
        }
    }
}
