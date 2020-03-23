using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;

namespace Tecan.Sila2.Interop.Server
{
    [Export(typeof(IDataTypeProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class DataTypeProvider : IDataTypeProvider
    {
        public string StringValue => "SiLA2_Test_String_Value";

        public long IntegerValue => 42;

        public double RealValue => 3.1415926;

        public bool BooleanValue => true;

        public Stream BinaryValue => new MemoryStream(Encoding.UTF8.GetBytes( "SiLA2_Test_String_Value" ) );

        public DateTimeOffset DateValue => new DateTimeOffset(new DateTime(2019, 8, 24));

        public TimeSpan TimeValue => new TimeSpan(12, 34, 56);

        public DateTimeOffset TimeStampValue => new DateTimeOffset(new DateTime(2018, 8, 24, 12, 34, 56));

        public ICollection<string> StringList
        {
            get;
        } = new List<string>() { "SiLA 2", "is", "great" };

        public ICollection<long> IntegerList
        {
            get;
        } = new List<long>() { 1, 2, 3 };

        public AllDatatypeStructure StructureValues => new AllDatatypeStructure( StringValue, IntegerValue, RealValue, BooleanValue, BinaryValue, DateValue, TimeValue, DateValue );

        public string EchoStringValue( string stringValue )
        {
            return stringValue;
        }

        public long EchoIntegerValue( long integerValue )
        {
            return integerValue;
        }

        public double EchoRealValue( double realValue )
        {
            return realValue;
        }

        public bool EchoBooleanValue( bool booleanValue )
        {
            return booleanValue;
        }

        public Stream EchoBinaryValue( Stream binaryValue )
        {
            return binaryValue;
        }

        public DateTimeOffset EchoDateValue( DateTimeOffset dateValue )
        {
            return dateValue;
        }

        public TimeSpan EchoTimeValue( TimeSpan timeValue )
        {
            return timeValue;
        }

        public DateTimeOffset EchoTimeStampValue( DateTimeOffset timeStampValue )
        {
            return timeStampValue;
        }

        public ICollection<string> EchoStringList( ICollection<string> stringList )
        {
            return stringList;
        }

        public long EchoIntegerList( ICollection<long> integerList )
        {
            return integerList?.Count ?? 0;
        }

        public void EchoStructureValues( AllDatatypeStructure receivedValues )
        {
        }

        public string EchoMultipleMixedValues( string stringValue, long integerValue, double realValue, bool booleanValue, DateTimeOffset dateValue, TimeSpan timeValue,
            DateTimeOffset timeStampValue )
        {
            return $"{stringValue},{integerValue},{realValue},{booleanValue},{dateValue},{timeValue},{timeStampValue}";
        }

        public GetMultipleMixedValuesResponse GetMultipleMixedValues()
        {
            return new GetMultipleMixedValuesResponse(StringValue, IntegerValue, RealValue, BooleanValue, DateValue, TimeValue, TimeStampValue);
        }
    }
}
