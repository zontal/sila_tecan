using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tecan.Sila2.Generator.Test.TestReference;

namespace Tecan.Sila2.DynamicClient.Test
{
    [TestFixture]
    public class DynamicSerializerTests
    {
        private static readonly Feature TestFeature = FeatureSerializer.LoadFromAssembly( typeof( DynamicSerializerTests ).Assembly, "Feature.xml" );

        [TestCase( "Test" )]
        public void DynamicSerializer_String_RoundtripWorks( string input )
        {
            var expectedBytes = ByteSerializer.ToByteArray( new StringDto( input ) );
            Assert.That( Roundtrip( BasicType.String, input, expectedBytes ), Is.EqualTo( input ) );
        }

        [TestCase( 8 )]
        [TestCase( 15 )]
        [TestCase( long.MaxValue )]
        public void DynamicSerializer_Integer_RoundtripWorks( long input )
        {
            var expectedBytes = ByteSerializer.ToByteArray( new IntegerDto( input ) );
            Assert.That( Roundtrip( BasicType.Integer, input, expectedBytes ), Is.EqualTo( input ) );
        }

        [Test]
        public void DynamicSerializer_IntegerList_RoundtripWorks()
        {
            var list = new List<long>()
            {
                1,
                8,
                15
            };
            var expectedBytes = ByteSerializer.ToByteArray( list.Select(it => new IntegerDto(it)).ToList() );
            Assert.That( Roundtrip( new DataTypeType { Item = new ListType { DataType = new DataTypeType { Item = BasicType.Integer } } }, list, expectedBytes ), Is.EquivalentTo( list ) );
        }

        [Test]
        public void DynamicSerializer_StringList_RoundtripWorks()
        {
            var list = new List<string>()
            {
                "Foo",
                "Bar"
            };
            var expectedBytes = ByteSerializer.ToByteArray( list.Select( it => new StringDto( it ) ).ToList() );
            Assert.That( Roundtrip( new DataTypeType { Item = new ListType { DataType = new DataTypeType { Item = BasicType.String } } }, list, expectedBytes ), Is.EquivalentTo( list ) );
        }

        [TestCase( 8.15 )]
        [TestCase( Math.PI )]
        [TestCase( double.NaN )]
        [TestCase( double.PositiveInfinity )]
        public void DynamicSerializer_Real_RoundtripWorks( double input )
        {
            var expectedBytes = ByteSerializer.ToByteArray( new RealDto( input ) );
            Assert.That( Roundtrip( BasicType.Real, input, expectedBytes ), Is.EqualTo( input ) );
        }

        [TestCase( BasicType.String, "Test" )]
        [TestCase( BasicType.Real, double.Epsilon )]
        public void DynamicSerializer_AnyType_RoundtripWorks( BasicType type, object input )
        {
            var inputProperty = new DynamicObjectProperty( null, null, null, new DataTypeType { Item = type } );
            inputProperty.Value = input;
            var expectedBytes = ByteSerializer.ToByteArray( new AnyTypeDto(inputProperty, null) );
            var result = (DynamicObjectProperty)Roundtrip( BasicType.Any, inputProperty, expectedBytes );
            Assert.That( result.Value, Is.EqualTo( input ) );
        }

        [TestCase( "Test" )]
        public void DynamicSerializer_PropertyResponse_RoundtripWorks( string input )
        {
            var expectedBytes = ByteSerializer.ToByteArray( new PropertyResponse<StringDto>(input) );
            var serializer = new DynamicObjectSerializer( type => TestFeature.Items.OfType<SiLAElement>().Single( t => t.Identifier == type ).DataType );
            var result = new DynamicObjectProperty( "Property", null, null, new DataTypeType { Item = BasicType.String } );
            serializer.Deserialize( result, expectedBytes, true, null );
            Assert.That( result.Value, Is.EqualTo( input ) );
        }

        [Test]
        public void DynamicSerializer_LogEntryData_RoundtripWorks()
        {
            var entry = new LogEntryData( "Foo", "Bar", null );
            var expectedBytes = ByteSerializer.ToByteArray( new LogEntryDataDto.InnerStruct( entry, null ) );
            var inputValue = CreateLogEntryData();
            var result = (DynamicObject)Roundtrip( LogEntryDataType.DataType, inputValue, expectedBytes );
            AssertLogEntryData( result );
        }

        private static void AssertLogEntryData( DynamicObject result )
        {
            Assert.That( result, Is.Not.Null );
            Assert.That( result.Elements.Count, Is.EqualTo( 2 ) );
            Assert.That( result.Elements[0].Value, Is.EqualTo( "Foo" ) );
            Assert.That( result.Elements[1].Value, Is.EqualTo( "Bar" ) );
        }

        [Test]
        public void DynamicSerializer_LogEntryDataAsCustomDataType_RoundtripWorks()
        {
            var entry = new LogEntryData( "Foo", "Bar", null );
            var expectedBytes = ByteSerializer.ToByteArray( new LogEntryDataDto( entry, null ) );
            var inputValue = CreateLogEntryData();
            Assert.DoesNotThrow( () => Roundtrip( new DataTypeType { Item = "LogEntryData" }, inputValue, expectedBytes ) );
        }

        [Test]
        public void DynamicSerializer_LogEntry_RoundtripWorks()
        {
            var entry = new LogEntry( "Message", "Channel", Severity.Info, new List<LogEntryData>()
            {
                new LogEntryData( "Foo", "Bar", null ),
                new LogEntryData( "Foo", "Bar", null )
            } );
            var expectedBytes = ByteSerializer.ToByteArray( new LogEntryDto.InnerStruct( entry, null ) );

            DynamicObject inputValue = CreateLogEntry();
            var result = (DynamicObject)Roundtrip( LogEntryType.DataType, inputValue, expectedBytes );

            AssertLogEntry( result );
        }

        private static void AssertLogEntry( DynamicObject result )
        {
            Assert.That( result, Is.Not.Null );
            Assert.That( result.Elements.Count, Is.EqualTo( 4 ) );
            Assert.That( result.Elements[0].Value, Is.EqualTo( "Message" ) );
            Assert.That( result.Elements[1].Value, Is.EqualTo( "Channel" ) );
            Assert.That( result.Elements[2].Value, Is.EqualTo( "Info" ) );
            Assert.That( result.Elements[3].Value, Is.Not.Empty );
            var entries = (result.Elements[3].Value as IEnumerable<object>).Cast<DynamicObject>().ToList();
            foreach(var entryData in entries)
            {
                AssertLogEntryData( entryData );
            }
            Assert.That( entries.Count, Is.EqualTo( 2 ) );
        }

        private DynamicObject CreateLogEntry()
        {
            var structure = (StructureType)LogEntryType.DataType.Item;
            return new DynamicObject
            {
                Elements =
                {
                    new DynamicObjectProperty(structure.Element[0]) {Value = "Message"},
                    new DynamicObjectProperty(structure.Element[1]) {Value = "Channel"},
                    new DynamicObjectProperty(structure.Element[2]) {Value = "Info"},
                    new DynamicObjectProperty(structure.Element[3])
                    {
                        Value = new List<object>()
                        {
                            CreateLogEntryData(),
                            CreateLogEntryData()
                        }
                    }
                }
            };
        }

        [Test]
        public void DynamicSerializer_LogEntryIntermediateResult_RoundtripWorks()
        {
            var entry = new LogEntry( "Message", "Channel", Severity.Info, new List<LogEntryData>()
            {
                new LogEntryData( "Foo", "Bar", null ),
                new LogEntryData( "Foo", "Bar", null )
            } );
            var expectedBytes = ByteSerializer.ToByteArray( new ListenForLogEntriesIntermediateDto( entry, null ) );
            var command = TestFeature.Items.OfType<FeatureCommand>().Single();
            var intermediateType = new DataTypeType()
            {
                Item = new StructureType()
                {
                    Element = command.IntermediateResponse
                }
            };
            var result = (DynamicObject)Roundtrip( intermediateType, new DynamicObject
            {
                Elements =
                {
                    new DynamicObjectProperty(command.IntermediateResponse[0])
                    {
                        Value = CreateLogEntry()
                    }
                }
            }, expectedBytes );

            Assert.That( result, Is.Not.Null );
            AssertLogEntry( (DynamicObject)result.Elements[0].Value );
        }

        private DynamicObject CreateLogEntryData()
        {
            SiLAElement logEntryType = LogEntryDataType;
            var structure = (StructureType)logEntryType.DataType.Item;
            return new DynamicObject
            {
                Elements =
                {
                    new DynamicObjectProperty(structure.Element[0]) { Value = "Foo" },
                    new DynamicObjectProperty(structure.Element[1]) { Value = "Bar" }
                }
            };
        }

        private static SiLAElement LogEntryType => TestFeature.Items.OfType<SiLAElement>().FirstOrDefault( e => e.Identifier == "LogEntry" );

        private static SiLAElement LogEntryDataType => TestFeature.Items.OfType<SiLAElement>().FirstOrDefault( e => e.Identifier == "LogEntryData" );

        private static object Roundtrip( BasicType dataType, object input, byte[] expectedBytes )
        {
            return Roundtrip( new DataTypeType { Item = dataType }, input, expectedBytes );
        }

        private static object Roundtrip( DataTypeType dataType, object input, byte[] expectedBytes )
        {
            var inputProperty = new DynamicObjectProperty( "source", null, null, dataType );
            inputProperty.Value = input;

            var serializer = new DynamicObjectSerializer( type => TestFeature.Items.OfType<SiLAElement>().Single(t => t.Identifier == type).DataType );

            var bytes = serializer.Serialize( inputProperty, null );
            Assert.That( bytes, Is.EquivalentTo( expectedBytes ) );

            var resultProperty = new DynamicObjectProperty( "result", null, null, inputProperty.Type );
            serializer.Deserialize( resultProperty, bytes, false, null );
            return resultProperty.Value;
        }
    }
}
