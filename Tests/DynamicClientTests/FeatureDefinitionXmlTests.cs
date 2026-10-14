using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace Tecan.Sila2.DynamicClient.Test
{
    /// <summary>
    /// Verifies the reflection-free feature definition codec behind FeatureSerializer, DataTypeSerializer and ConstraintSerializer
    /// against the XmlSerializer, which the serializers used before and which serves as the oracle here.
    /// </summary>
    [TestFixture]
    public class FeatureDefinitionXmlTests
    {
        private const string SilaNamespace = "http://www.sila-standard.org";

        private static readonly XmlSerializer FeatureOracle = new XmlSerializer( typeof( Feature ) );
        private static readonly XmlSerializer DataTypeOracle = new XmlSerializer( typeof( DataTypeType ), new XmlRootAttribute( "DataType" ) { Namespace = SilaNamespace } );
        private static readonly XmlSerializer ConstraintsOracle = new XmlSerializer( typeof( Constraints ) );

        private static readonly XmlWriterSettings DataTypeWriterSettings = new XmlWriterSettings
        {
            Indent = false,
            OmitXmlDeclaration = true,
        };

        private sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => new UTF8Encoding( false );
        }

        #region Oracle

        private static string OracleSaveFeature( Feature feature )
        {
            using(var writer = new Utf8StringWriter())
            {
                FeatureOracle.Serialize( writer, feature );
                return writer.ToString();
            }
        }

        private static Feature OracleLoadFeature( string xml )
        {
            return (Feature)FeatureOracle.Deserialize( new StringReader( xml ) );
        }

        private static string OracleSaveDataType( DataTypeType dataType )
        {
            using(var writer = new Utf8StringWriter())
            {
                using(var xmlWriter = XmlWriter.Create( writer, DataTypeWriterSettings ))
                {
                    DataTypeOracle.Serialize( xmlWriter, dataType );
                    return writer.ToString();
                }
            }
        }

        private static DataTypeType OracleLoadDataType( string xml )
        {
            return (DataTypeType)DataTypeOracle.Deserialize( new StringReader( xml ) );
        }

        private static string OracleSaveConstraints( Constraints constraints )
        {
            using(var writer = new Utf8StringWriter())
            {
                ConstraintsOracle.Serialize( writer, constraints );
                return writer.ToString();
            }
        }

        private static Constraints OracleLoadConstraints( string xml )
        {
            return ConstraintsOracle.Deserialize( new StringReader( xml ) ) as Constraints;
        }

        #endregion

        #region Deep equality

        private static void AssertDeepEqual( object expected, object actual, string path = "$" )
        {
            if(expected == null || actual == null)
            {
                Assert.That( actual, Is.EqualTo( expected ), path );
                return;
            }
            Assert.That( actual.GetType(), Is.EqualTo( expected.GetType() ), path + " (type)" );
            var type = expected.GetType();
            if(type.IsPrimitive || type.IsEnum || expected is string || expected is decimal)
            {
                Assert.That( actual, Is.EqualTo( expected ), path );
                return;
            }
            if(expected is IList expectedList)
            {
                var actualList = (IList)actual;
                Assert.That( actualList.Count, Is.EqualTo( expectedList.Count ), path + " (count)" );
                for(var i = 0; i < expectedList.Count; i++)
                {
                    AssertDeepEqual( expectedList[i], actualList[i], $"{path}[{i}]" );
                }
                return;
            }
            foreach(var property in type.GetProperties( BindingFlags.Public | BindingFlags.Instance ).Where( p => p.CanRead && p.GetIndexParameters().Length == 0 ))
            {
                object expectedValue;
                try
                {
                    expectedValue = property.GetValue( expected );
                }
                catch(TargetInvocationException)
                {
                    // computed properties such as Feature.FullyQualifiedIdentifier may throw on incomplete objects
                    Assert.Throws<TargetInvocationException>( () => property.GetValue( actual ), path + "." + property.Name );
                    continue;
                }
                AssertDeepEqual( expectedValue, property.GetValue( actual ), path + "." + property.Name );
            }
        }

        private static void AssertSameOutcome<T>( Func<T> oracle, Func<T> actual )
        {
            T expectedResult = default;
            Exception expectedException = null;
            try
            {
                expectedResult = oracle();
            }
            catch(Exception exception)
            {
                expectedException = exception;
            }

            if(expectedException != null)
            {
                var actualException = Assert.Catch( () => actual() );
                Assert.That( actualException, Is.InstanceOf( expectedException.GetType() ), "Oracle threw " + expectedException );
            }
            else
            {
                T actualResult = default;
                Assert.DoesNotThrow( () => actualResult = actual() );
                AssertDeepEqual( expectedResult, actualResult );
            }
        }

        #endregion

        #region Corpus

        private static string RepositoryRoot
        {
            get
            {
                var directory = new DirectoryInfo( TestContext.CurrentContext.TestDirectory );
                while(directory != null && !File.Exists( Path.Combine( directory.FullName, "TecanSilaSDK.sln" ) ))
                {
                    directory = directory.Parent;
                }
                return directory?.FullName ?? throw new InvalidOperationException( "Repository root not found" );
            }
        }

        /// <summary>
        /// Gets the text of the file from its first markup on, as FeatureSerializer.LoadFromXml reads it
        /// </summary>
        private static string ReadMarkup( string path )
        {
            var text = File.ReadAllText( path );
            return text.Substring( Math.Max( 0, text.IndexOf( '<' ) ) );
        }

        private static bool IsFeatureDefinition( string path )
        {
            try
            {
                using(var reader = XmlReader.Create( new StringReader( ReadMarkup( path ) ), new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore } ))
                {
                    reader.MoveToContent();
                    return reader.LocalName == "Feature" && reader.NamespaceURI == SilaNamespace;
                }
            }
            catch(XmlException)
            {
                return false;
            }
        }

        private static IEnumerable<string> EnumerateXmlFiles( string directory )
        {
            foreach(var file in Directory.EnumerateFiles( directory, "*.xml" ))
            {
                yield return file;
            }
            foreach(var subDirectory in Directory.EnumerateDirectories( directory ))
            {
                var name = Path.GetFileName( subDirectory );
                if(name == "bin" || name == "obj" || name.StartsWith( "." ) || name == "Build" || name == "TestResults")
                {
                    continue;
                }
                foreach(var file in EnumerateXmlFiles( subDirectory ))
                {
                    yield return file;
                }
            }
        }

        private static readonly Lazy<string[]> CorpusFiles = new Lazy<string[]>( () =>
            EnumerateXmlFiles( RepositoryRoot ).Where( IsFeatureDefinition ).OrderBy( f => f, StringComparer.Ordinal ).ToArray() );

        public static IEnumerable<TestCaseData> FeatureFiles()
        {
            var root = RepositoryRoot;
            return CorpusFiles.Value.Select( f => new TestCaseData( f ).SetArgDisplayNames( f.Substring( root.Length + 1 ).Replace( '\\', '/' ) ) );
        }

        private static IEnumerable<object> Walk( object value )
        {
            if(value == null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum || value is decimal)
            {
                yield break;
            }
            yield return value;
            if(value is IList list)
            {
                foreach(var item in list)
                {
                    foreach(var nested in Walk( item ))
                    {
                        yield return nested;
                    }
                }
                yield break;
            }
            foreach(var property in value.GetType().GetProperties( BindingFlags.Public | BindingFlags.Instance ).Where( p => p.CanWrite ))
            {
                foreach(var nested in Walk( property.GetValue( value ) ))
                {
                    yield return nested;
                }
            }
        }

        [Test]
        public void Corpus_ContainsAllFeatureDefinitionsOfTheRepository()
        {
            Assert.That( CorpusFiles.Value.Length, Is.GreaterThanOrEqualTo( 46 ) );
            Assert.That( CorpusFiles.Value.Count( f => f.EndsWith( ".sila.xml", StringComparison.OrdinalIgnoreCase ) ), Is.GreaterThanOrEqualTo( 37 ) );
        }

        [TestCaseSource( nameof( FeatureFiles ) )]
        public void Feature_LoadFromFile_EqualsXmlSerializer( string path )
        {
            var expected = OracleLoadFeature( ReadMarkup( path ) );
            AssertDeepEqual( expected, FeatureSerializer.Load( path ) );
            AssertDeepEqual( expected, FeatureSerializer.LoadFromXml( File.ReadAllText( path ) ) );
            AssertSameOutcome( () =>
            {
                using(var reader = new StreamReader( path ))
                {
                    return (Feature)FeatureOracle.Deserialize( reader );
                }
            }, () =>
            {
                using(var stream = File.OpenRead( path ))
                {
                    return FeatureSerializer.Load( stream );
                }
            } );
        }

        [TestCaseSource( nameof( FeatureFiles ) )]
        public void Feature_SaveToString_EqualsXmlSerializer( string path )
        {
            var feature = FeatureSerializer.Load( path );
            var xml = FeatureSerializer.SaveToString( feature );
            Assert.That( xml, Is.EqualTo( OracleSaveFeature( feature ) ) );
            AssertDeepEqual( feature, OracleLoadFeature( xml ) );
        }

        [TestCaseSource( nameof( FeatureFiles ) )]
        public void Feature_SaveToFile_EqualsXmlSerializer( string path )
        {
            var feature = FeatureSerializer.Load( path );
            var actualPath = Path.Combine( TestContext.CurrentContext.WorkDirectory, Guid.NewGuid() + ".sila.xml" );
            var expectedPath = Path.Combine( TestContext.CurrentContext.WorkDirectory, Guid.NewGuid() + ".sila.xml" );
            try
            {
                FeatureSerializer.Save( feature, actualPath );
                using(var writer = new StreamWriter( expectedPath ))
                {
                    FeatureOracle.Serialize( writer, feature );
                }
                Assert.That( File.ReadAllBytes( actualPath ), Is.EqualTo( File.ReadAllBytes( expectedPath ) ) );
            }
            finally
            {
                File.Delete( actualPath );
                File.Delete( expectedPath );
            }
        }

        [TestCaseSource( nameof( FeatureFiles ) )]
        public void DataTypesAndConstraintsOfFeature_EqualXmlSerializer( string path )
        {
            var feature = OracleLoadFeature( ReadMarkup( path ) );
            var nodes = Walk( feature ).ToList();
            foreach(var dataType in nodes.OfType<DataTypeType>())
            {
                AssertDataTypeEqualsOracle( dataType );
            }
            foreach(var constraints in nodes.OfType<Constraints>())
            {
                AssertConstraintsEqualsOracle( constraints );
            }
        }

        private static void AssertDataTypeEqualsOracle( DataTypeType dataType )
        {
            var expectedXml = OracleSaveDataType( dataType );
            var actualXml = DataTypeSerializer.SaveToString( dataType );
            Assert.That( actualXml, Is.EqualTo( expectedXml ) );
            AssertDeepEqual( OracleLoadDataType( expectedXml ), DataTypeSerializer.LoadType( expectedXml ) );
            AssertDeepEqual( OracleLoadDataType( expectedXml ), OracleLoadDataType( actualXml ) );
        }

        private static void AssertConstraintsEqualsOracle( Constraints constraints )
        {
            var expectedXml = OracleSaveConstraints( constraints );
            var actualXml = ConstraintSerializer.SaveToString( constraints );
            Assert.That( actualXml, Is.EqualTo( expectedXml ) );
            AssertDeepEqual( OracleLoadConstraints( expectedXml ), ConstraintSerializer.LoadConstraints( expectedXml ) );
            AssertDeepEqual( OracleLoadConstraints( expectedXml ), OracleLoadConstraints( actualXml ) );
        }

        #endregion

        #region Data types

        private static DataTypeType Basic( BasicType basicType )
        {
            return new DataTypeType { Item = basicType };
        }

        private static DataTypeType Constrained( DataTypeType dataType, Constraints constraints )
        {
            return new DataTypeType { Item = new ConstrainedType { DataType = dataType, Constraints = constraints } };
        }

        private static SiLAElement Element( string identifier, DataTypeType dataType, string description = "A description" )
        {
            return new SiLAElement { Identifier = identifier, DisplayName = identifier + " name", Description = description, DataType = dataType };
        }

        private static Constraints AllConstraints()
        {
            return new Constraints
            {
                Length = "5",
                MinimalLength = "1",
                MaximalLength = "10",
                Set = new[] { "A", "B & C", "<D>", "\"E\"", string.Empty },
                Pattern = "[a-z]+<>&'\"\\d{2,}",
                MaximalExclusive = "100",
                MaximalInclusive = "99.5",
                MinimalExclusive = "-1",
                MinimalInclusive = "2022-08-05T12:00:00+01:00",
                Unit = new ConstraintsUnit
                {
                    Label = "mL/s",
                    Factor = 0.000001m,
                    Offset = -273.15m,
                    UnitComponent = new[]
                    {
                        new ConstraintsUnitUnitComponent { SIUnit = ConstraintsUnitUnitComponentSIUnit.Meter, Exponent = "3" },
                        new ConstraintsUnitUnitComponent { SIUnit = ConstraintsUnitUnitComponentSIUnit.Second, Exponent = "-1" },
                    }
                },
                ContentType = new ConstraintsContentType
                {
                    Type = "application",
                    Subtype = "xml",
                    Parameters = new[]
                    {
                        new ConstraintsContentTypeParameter { Attribute = "charset", Value = "utf-8" },
                        new ConstraintsContentTypeParameter { Attribute = "version", Value = "1.0" },
                    }
                },
                ElementCount = "3",
                MinimalElementCount = "1",
                MaximalElementCount = "7",
                FullyQualifiedIdentifier = ConstraintsFullyQualifiedIdentifier.IntermediateCommandResponseIdentifier,
                FullyQualifiedIdentifierSpecified = true,
                Schema = new ConstraintsSchema { Type = ConstraintsSchemaType.Json, Item = "https://example.org/schema.json", ItemElementName = ItemChoiceType.Url },
                AllowedTypes = new[] { Basic( BasicType.Integer ), Basic( BasicType.Real ), new DataTypeType { Item = new ListType { DataType = Basic( BasicType.String ) } } },
            };
        }

        public static IEnumerable<TestCaseData> DataTypes()
        {
            foreach(BasicType basicType in Enum.GetValues( typeof( BasicType ) ))
            {
                yield return new TestCaseData( Basic( basicType ) ).SetArgDisplayNames( "Basic " + basicType );
            }
            yield return new TestCaseData( new DataTypeType() ).SetArgDisplayNames( "Empty" );
            yield return new TestCaseData( new DataTypeType { Item = "MyType" } ).SetArgDisplayNames( "Identifier" );
            yield return new TestCaseData( new DataTypeType { Item = string.Empty } ).SetArgDisplayNames( "Empty identifier" );
            yield return new TestCaseData( new DataTypeType { Item = new ListType() } ).SetArgDisplayNames( "Empty list" );
            yield return new TestCaseData( new DataTypeType { Item = new ListType { DataType = new DataTypeType { Item = new ListType { DataType = Basic( BasicType.Binary ) } } } } ).SetArgDisplayNames( "List of lists" );
            yield return new TestCaseData( new DataTypeType { Item = new StructureType() } ).SetArgDisplayNames( "Empty structure" );
            yield return new TestCaseData( new DataTypeType { Item = new StructureType { Element = new SiLAElement[0] } } ).SetArgDisplayNames( "Structure with empty array" );
            yield return new TestCaseData( new DataTypeType
            {
                Item = new StructureType
                {
                    Element = new[]
                    {
                        Element( "Name", Basic( BasicType.String ), "Contains <markup> & \"quotes\"\r\nand line breaks\n" ),
                        null,
                        Element( "Values", new DataTypeType { Item = new ListType { DataType = Constrained( Basic( BasicType.Real ), new Constraints { MinimalInclusive = "0" } ) } } ),
                        Element( "Nested", new DataTypeType { Item = new StructureType { Element = new[] { Element( "Inner", new DataTypeType { Item = "OtherType" } ) } } } ),
                        new SiLAElement(),
                        new SiLAElement { Identifier = string.Empty, DisplayName = " padded ", Description = "\t" },
                    }
                }
            } ).SetArgDisplayNames( "Nested structure" );
            yield return new TestCaseData( Constrained( Basic( BasicType.String ), AllConstraints() ) ).SetArgDisplayNames( "All constraints" );
            yield return new TestCaseData( Constrained( Basic( BasicType.String ), new Constraints() ) ).SetArgDisplayNames( "No constraints" );
            yield return new TestCaseData( new DataTypeType { Item = new ConstrainedType() } ).SetArgDisplayNames( "Empty constrained" );
            yield return new TestCaseData( Constrained( Basic( BasicType.String ), new Constraints { Length = "0" } ) ).SetArgDisplayNames( "Length 0" );
            yield return new TestCaseData( Constrained( Basic( BasicType.String ), new Constraints { Set = new string[0], AllowedTypes = new DataTypeType[0] } ) ).SetArgDisplayNames( "Empty set" );
            yield return new TestCaseData( Constrained( Basic( BasicType.String ), new Constraints { Set = new[] { "A", null, "B" }, AllowedTypes = new[] { null, Basic( BasicType.Any ) } } ) ).SetArgDisplayNames( "Set with null" );
            yield return new TestCaseData( Constrained( Basic( BasicType.String ), new Constraints { Pattern = "\u00e4\u00f6\u00fc \u20ac \U0001F600 \u00a0" } ) ).SetArgDisplayNames( "Unicode pattern" );
            yield return new TestCaseData( Constrained( Basic( BasicType.String ), new Constraints { FullyQualifiedIdentifier = ConstraintsFullyQualifiedIdentifier.MetadataIdentifier } ) ).SetArgDisplayNames( "FQI not specified" );
            foreach(ConstraintsFullyQualifiedIdentifier identifier in Enum.GetValues( typeof( ConstraintsFullyQualifiedIdentifier ) ))
            {
                yield return new TestCaseData( Constrained( Basic( BasicType.String ), new Constraints { FullyQualifiedIdentifier = identifier, FullyQualifiedIdentifierSpecified = true } ) ).SetArgDisplayNames( "FQI " + identifier );
            }
            foreach(ConstraintsUnitUnitComponentSIUnit unit in Enum.GetValues( typeof( ConstraintsUnitUnitComponentSIUnit ) ))
            {
                yield return new TestCaseData( Constrained( Basic( BasicType.Real ), new Constraints { Unit = new ConstraintsUnit { Label = unit.ToString(), Factor = 1, UnitComponent = new[] { new ConstraintsUnitUnitComponent { SIUnit = unit, Exponent = "1" } } } } ) ).SetArgDisplayNames( "Unit " + unit );
            }
            yield return new TestCaseData( Constrained( Basic( BasicType.Real ), new Constraints { Unit = new ConstraintsUnit() } ) ).SetArgDisplayNames( "Empty unit" );
            yield return new TestCaseData( Constrained( Basic( BasicType.Real ), new Constraints { Unit = new ConstraintsUnit { Factor = 1234567890.0123456789m, Offset = -0.5m, UnitComponent = new ConstraintsUnitUnitComponent[] { null, new ConstraintsUnitUnitComponent() } } } ) ).SetArgDisplayNames( "Unit precision" );
            yield return new TestCaseData( Constrained( Basic( BasicType.Binary ), new Constraints { ContentType = new ConstraintsContentType() } ) ).SetArgDisplayNames( "Empty content type" );
            yield return new TestCaseData( Constrained( Basic( BasicType.Binary ), new Constraints { ContentType = new ConstraintsContentType { Type = "text", Subtype = "plain", Parameters = new ConstraintsContentTypeParameter[0] } } ) ).SetArgDisplayNames( "Content type without parameters" );
            yield return new TestCaseData( Constrained( Basic( BasicType.Binary ), new Constraints { Schema = new ConstraintsSchema { Type = ConstraintsSchemaType.Xml, Item = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"/>", ItemElementName = ItemChoiceType.Inline } } ) ).SetArgDisplayNames( "Inline schema" );
            yield return new TestCaseData( Constrained( Basic( BasicType.Binary ), new Constraints { Schema = new ConstraintsSchema() } ) ).SetArgDisplayNames( "Empty schema" );
            yield return new TestCaseData( Constrained( new DataTypeType { Item = new ListType { DataType = Basic( BasicType.Integer ) } }, new Constraints { ElementCount = "2", MinimalElementCount = "1", MaximalElementCount = "3" } ) ).SetArgDisplayNames( "Constrained list" );
            yield return new TestCaseData( Constrained( Basic( BasicType.Any ), new Constraints { AllowedTypes = new[] { Basic( BasicType.Integer ), new DataTypeType { Item = new StructureType { Element = new[] { Element( "A", Basic( BasicType.Boolean ) ) } } } } } ) ).SetArgDisplayNames( "Allowed types" );
            yield return new TestCaseData( Constrained( Basic( BasicType.Timestamp ), new Constraints { MinimalExclusive = "2000-01-01T00:00:00Z", MaximalExclusive = "2100-01-01T00:00:00Z" } ) ).SetArgDisplayNames( "Timestamp range" );
            yield return new TestCaseData( Constrained( Constrained( Basic( BasicType.Integer ), new Constraints { MinimalInclusive = "0" } ), new Constraints { MaximalInclusive = "10" } ) ).SetArgDisplayNames( "Constrained constrained" );

            var random = new Random( 4711 );
            for(var i = 0; i < 200; i++)
            {
                yield return new TestCaseData( RandomDataType( random, 0 ) ).SetArgDisplayNames( "Random " + i );
            }
        }

        private static string RandomString( Random random )
        {
            const string characters = "abcXYZ019 _-<>&\"'\r\n\t\u00e4\u20ac";
            if(random.Next( 8 ) == 0)
            {
                return null;
            }
            var length = random.Next( 6 );
            return new string( Enumerable.Range( 0, length ).Select( _ => characters[random.Next( characters.Length )] ).ToArray() );
        }

        private static T Pick<T>( Random random, T[] values )
        {
            return values[random.Next( values.Length )];
        }

        private static T RandomEnum<T>( Random random )
        {
            return Pick( random, Enum.GetValues( typeof( T ) ).Cast<T>().ToArray() );
        }

        private static string[] RandomArray( Random random )
        {
            switch(random.Next( 3 ))
            {
                case 0: return null;
                case 1: return new string[0];
                default: return Enumerable.Range( 0, 1 + random.Next( 3 ) ).Select( _ => RandomString( random ) ?? "x" ).ToArray();
            }
        }

        private static Constraints RandomConstraints( Random random, int depth )
        {
            var constraints = new Constraints();
            if(random.Next( 3 ) == 0) constraints.Length = random.Next( 100 ).ToString();
            if(random.Next( 3 ) == 0) constraints.MinimalLength = random.Next( 100 ).ToString();
            if(random.Next( 3 ) == 0) constraints.MaximalLength = random.Next( 100 ).ToString();
            if(random.Next( 3 ) == 0) constraints.Set = RandomArray( random );
            if(random.Next( 3 ) == 0) constraints.Pattern = RandomString( random );
            if(random.Next( 3 ) == 0) constraints.MaximalExclusive = random.Next( -100, 100 ).ToString();
            if(random.Next( 3 ) == 0) constraints.MaximalInclusive = RandomString( random );
            if(random.Next( 3 ) == 0) constraints.MinimalExclusive = RandomString( random );
            if(random.Next( 3 ) == 0) constraints.MinimalInclusive = random.Next( -100, 100 ).ToString();
            if(random.Next( 4 ) == 0)
            {
                constraints.Unit = new ConstraintsUnit
                {
                    Label = RandomString( random ),
                    Factor = (decimal)random.NextDouble() * random.Next( 1000 ),
                    Offset = random.Next( -500, 500 ) / 7m,
                    UnitComponent = random.Next( 3 ) == 0 ? null : Enumerable.Range( 0, random.Next( 3 ) ).Select( _ => new ConstraintsUnitUnitComponent { SIUnit = RandomEnum<ConstraintsUnitUnitComponentSIUnit>( random ), Exponent = random.Next( -3, 4 ).ToString() } ).ToArray()
                };
            }
            if(random.Next( 4 ) == 0)
            {
                constraints.ContentType = new ConstraintsContentType
                {
                    Type = RandomString( random ),
                    Subtype = RandomString( random ),
                    Parameters = random.Next( 3 ) == 0 ? null : Enumerable.Range( 0, random.Next( 3 ) ).Select( _ => new ConstraintsContentTypeParameter { Attribute = RandomString( random ), Value = RandomString( random ) } ).ToArray()
                };
            }
            if(random.Next( 3 ) == 0) constraints.ElementCount = random.Next( 100 ).ToString();
            if(random.Next( 3 ) == 0) constraints.MinimalElementCount = random.Next( 100 ).ToString();
            if(random.Next( 3 ) == 0) constraints.MaximalElementCount = random.Next( 100 ).ToString();
            if(random.Next( 3 ) == 0)
            {
                constraints.FullyQualifiedIdentifier = RandomEnum<ConstraintsFullyQualifiedIdentifier>( random );
                constraints.FullyQualifiedIdentifierSpecified = random.Next( 4 ) != 0;
            }
            if(random.Next( 4 ) == 0)
            {
                constraints.Schema = new ConstraintsSchema { Type = RandomEnum<ConstraintsSchemaType>( random ), Item = RandomString( random ), ItemElementName = RandomEnum<ItemChoiceType>( random ) };
            }
            if(depth < 3 && random.Next( 5 ) == 0)
            {
                constraints.AllowedTypes = Enumerable.Range( 0, random.Next( 3 ) ).Select( _ => RandomDataType( random, depth + 1 ) ).ToArray();
            }
            return constraints;
        }

        private static DataTypeType RandomDataType( Random random, int depth )
        {
            switch(depth >= 3 ? random.Next( 2 ) : random.Next( 6 ))
            {
                case 0:
                    return Basic( RandomEnum<BasicType>( random ) );
                case 1:
                    return new DataTypeType { Item = RandomString( random ) ?? "Identifier" };
                case 2:
                    return new DataTypeType { Item = new ListType { DataType = RandomDataType( random, depth + 1 ) } };
                case 3:
                    return new DataTypeType
                    {
                        Item = new StructureType
                        {
                            Element = Enumerable.Range( 0, random.Next( 4 ) ).Select( i => new SiLAElement
                            {
                                Identifier = RandomString( random ),
                                DisplayName = RandomString( random ),
                                Description = RandomString( random ),
                                DataType = RandomDataType( random, depth + 1 )
                            } ).ToArray()
                        }
                    };
                case 4:
                    return Constrained( RandomDataType( random, depth + 1 ), RandomConstraints( random, depth ) );
                default:
                    return new DataTypeType();
            }
        }

        [TestCaseSource( nameof( DataTypes ) )]
        public void DataType_SaveAndLoad_EqualsXmlSerializer( DataTypeType dataType )
        {
            AssertDataTypeEqualsOracle( dataType );
            if(dataType.Item is ConstrainedType constrained && constrained.Constraints != null)
            {
                AssertConstraintsEqualsOracle( constrained.Constraints );
            }
        }

        [Test]
        public void DataType_SaveToString_HasExpectedWireFormat()
        {
            var xml = DataTypeSerializer.SaveToString( Constrained( Basic( BasicType.String ), new Constraints { Length = "0" } ) );

            Assert.That( xml, Does.StartWith( "<DataType xmlns:" ) );
            Assert.That( xml, Does.EndWith( " xmlns=\"http://www.sila-standard.org\"><Constrained><DataType><Basic>String</Basic></DataType><Constraints><Length>0</Length></Constraints></Constrained></DataType>" ) );
        }

        [Test]
        public void Null_SaveAndLoad_EqualsXmlSerializer()
        {
            Assert.That( DataTypeSerializer.SaveToString( null ), Is.EqualTo( OracleSaveDataType( null ) ) );
            Assert.That( ConstraintSerializer.SaveToString( null ), Is.EqualTo( OracleSaveConstraints( null ) ) );
            Assert.That( FeatureSerializer.SaveToString( null ), Is.EqualTo( OracleSaveFeature( null ) ) );
            AssertSameOutcome( () => OracleLoadDataType( OracleSaveDataType( null ) ), () => DataTypeSerializer.LoadType( OracleSaveDataType( null ) ) );
            AssertSameOutcome( () => OracleLoadConstraints( OracleSaveConstraints( null ) ), () => ConstraintSerializer.LoadConstraints( OracleSaveConstraints( null ) ) );
            AssertSameOutcome( () => OracleLoadFeature( OracleSaveFeature( null ) ), () => FeatureSerializer.LoadFromXml( OracleSaveFeature( null ) ) );
        }

        #endregion

        #region Features

        private static Feature CompleteFeature()
        {
            return new Feature
            {
                Locale = "de-de",
                SiLA2Version = "1.1",
                FeatureVersion = "2.3",
                MaturityLevel = FeatureMaturityLevel.Verified,
                Originator = "org.example",
                Category = "tests",
                Identifier = "CompleteFeature",
                DisplayName = "Complete Feature",
                Description = "Uses every element & attribute <of> the model",
                Items = new object[]
                {
                    new FeatureCommand
                    {
                        Identifier = "Run",
                        DisplayName = "Run",
                        Description = "Runs",
                        Observable = FeatureCommandObservable.Yes,
                        Parameter = new[] { Element( "A", Basic( BasicType.Integer ) ), Element( "B", Constrained( Basic( BasicType.String ), AllConstraints() ) ) },
                        Response = new[] { Element( "R", new DataTypeType { Item = "MyType" } ) },
                        IntermediateResponse = new[] { Element( "I", new DataTypeType { Item = new ListType { DataType = Basic( BasicType.Real ) } } ) },
                        DefinedExecutionErrors = new[] { "Error1", "Error2" }
                    },
                    new FeatureCommand { Identifier = "Empty", DefinedExecutionErrors = new string[0], Parameter = new SiLAElement[0] },
                    null,
                    new FeatureProperty { Identifier = "P", Observable = FeaturePropertyObservable.Yes, DataType = Basic( BasicType.Boolean ), DefinedExecutionErrors = new[] { "Error1" } },
                    new FeatureProperty { Identifier = "Q" },
                    new FeatureMetadata { Identifier = "M", DataType = Basic( BasicType.Binary ), DefinedExecutionErrors = new string[0] },
                    new FeatureMetadata(),
                    new FeatureDefinedExecutionError { Identifier = "Error1", DisplayName = "Error 1", Description = "First" },
                    new FeatureDefinedExecutionError(),
                    Element( "MyType", new DataTypeType { Item = new StructureType { Element = new[] { Element( "X", Basic( BasicType.Date ) ), Element( "Y", Basic( BasicType.Time ) ) } } } ),
                }
            };
        }

        public static IEnumerable<TestCaseData> Features()
        {
            yield return new TestCaseData( CompleteFeature() ).SetArgDisplayNames( "Complete" );
            yield return new TestCaseData( new Feature() ).SetArgDisplayNames( "Default" );
            yield return new TestCaseData( new Feature { Locale = null, Category = null, Items = new object[0] } ).SetArgDisplayNames( "Null defaults" );
            yield return new TestCaseData( new Feature { Locale = "en-us", Category = "none", MaturityLevel = FeatureMaturityLevel.Draft, Items = new object[] { null } } ).SetArgDisplayNames( "Explicit defaults" );
            yield return new TestCaseData( new Feature { MaturityLevel = FeatureMaturityLevel.Normative, Category = string.Empty, Locale = string.Empty } ).SetArgDisplayNames( "Empty attributes" );
            var random = new Random( 815 );
            for(var i = 0; i < 25; i++)
            {
                yield return new TestCaseData( new Feature
                {
                    Identifier = RandomString( random ),
                    Originator = RandomString( random ),
                    Category = RandomString( random ),
                    Description = RandomString( random ),
                    Items = Enumerable.Range( 0, random.Next( 6 ) ).Select( _ => random.Next( 3 ) == 0
                        ? (object)Element( RandomString( random ), RandomDataType( random, 0 ) )
                        : new FeatureCommand
                        {
                            Identifier = RandomString( random ),
                            Observable = RandomEnum<FeatureCommandObservable>( random ),
                            Parameter = Enumerable.Range( 0, random.Next( 3 ) ).Select( p => Element( RandomString( random ), RandomDataType( random, 0 ) ) ).ToArray(),
                            DefinedExecutionErrors = RandomArray( random ),
                        } ).ToArray()
                } ).SetArgDisplayNames( "Random " + i );
            }
        }

        [TestCaseSource( nameof( Features ) )]
        public void Feature_SaveAndLoad_EqualsXmlSerializer( Feature feature )
        {
            var expectedXml = OracleSaveFeature( feature );
            var actualXml = FeatureSerializer.SaveToString( feature );
            Assert.That( actualXml, Is.EqualTo( expectedXml ) );
            AssertDeepEqual( OracleLoadFeature( expectedXml ), FeatureSerializer.LoadFromXml( expectedXml ) );
        }

        private class DerivedCommand : FeatureCommand
        {
        }

        public static IEnumerable<TestCaseData> InvalidModels()
        {
            yield return new TestCaseData( new Feature { Items = new object[] { "unexpected" } } ).SetArgDisplayNames( "Unexpected item" );
            yield return new TestCaseData( new Feature { Items = new object[] { new DerivedCommand() } } ).SetArgDisplayNames( "Derived item" );
            yield return new TestCaseData( new Feature { MaturityLevel = (FeatureMaturityLevel)42 } ).SetArgDisplayNames( "Invalid enum" );
            yield return new TestCaseData( new Feature { Items = new object[] { Element( "A", new DataTypeType { Item = 42 } ) } } ).SetArgDisplayNames( "Invalid data type item" );
            yield return new TestCaseData( new Feature { Items = new object[] { Element( "A", Basic( (BasicType)42 ) ) } } ).SetArgDisplayNames( "Invalid basic type" );
            yield return new TestCaseData( new Feature { Items = new object[] { Element( "A", Constrained( Basic( BasicType.String ), new Constraints { Schema = new ConstraintsSchema { Item = "x", ItemElementName = (ItemChoiceType)42 } } ) ) } } ).SetArgDisplayNames( "Invalid choice" );
        }

        [TestCaseSource( nameof( InvalidModels ) )]
        public void Feature_SaveInvalidModel_ThrowsLikeXmlSerializer( Feature feature )
        {
            var expected = Assert.Catch( () => OracleSaveFeature( feature ) );
            var actual = Assert.Catch( () => FeatureSerializer.SaveToString( feature ) );
            Assert.That( actual, Is.InstanceOf( expected.GetType() ) );
            Assert.That( actual.InnerException, Is.InstanceOf( expected.InnerException.GetType() ) );
        }

        [Test]
        public void Feature_SaveInvalidCharacter_BehavesLikeXmlSerializer()
        {
            var feature = new Feature { Description = "invalid \u0001 character" };
            AssertSameOutcome( () => OracleSaveFeature( feature ), () => FeatureSerializer.SaveToString( feature ) );
        }

        #endregion

        #region Parsing edge cases

        private const string FeatureStart = "<Feature SiLA2Version=\"1.0\" FeatureVersion=\"1.0\" Originator=\"org\" Category=\"cat\" xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">";

        public static IEnumerable<TestCaseData> FeatureDocuments()
        {
            var cases = new Dictionary<string, string>
            {
                ["Minimal"] = "<Feature xmlns=\"http://www.sila-standard.org\"/>",
                ["Unknown root"] = "<Other xmlns=\"http://www.sila-standard.org\"/>",
                ["Wrong namespace"] = "<Feature/>",
                ["Leading whitespace and comments"] = "  \r\n<!-- c --><?pi x?>" + FeatureStart + "<Identifier>A</Identifier></Feature>",
                ["Xml declaration"] = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" + FeatureStart + "</Feature>",
                ["Unknown attributes"] = "<Feature Foo=\"bar\" xsi:schemaLocation=\"x\" xmlns:x=\"urn:x\" x:Locale=\"xx\" xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"/>",
                ["Attribute normalization"] = "<Feature Originator=\" a\tb\r\nc \" Locale=\"&#10;x\" xmlns=\"http://www.sila-standard.org\"/>",
                ["Invalid maturity"] = "<Feature MaturityLevel=\"draft\" xmlns=\"http://www.sila-standard.org\"/>",
                ["All maturities"] = "<Feature MaturityLevel=\"Normative\" xmlns=\"http://www.sila-standard.org\"/>",
                ["Unknown elements"] = FeatureStart + "<Foo><Identifier>X</Identifier></Foo><Identifier>A</Identifier><x:Identifier xmlns:x=\"urn:x\">B</x:Identifier>text<![CDATA[cdata]]><Bar/></Feature>",
                ["Duplicate elements"] = FeatureStart + "<Identifier>A</Identifier><Identifier>B</Identifier><Description>1</Description><Description>2</Description></Feature>",
                ["Out of order"] = FeatureStart + "<Command><Identifier>C</Identifier><Observable>No</Observable></Command><Description>D</Description><Identifier>A</Identifier><Property><Identifier>P</Identifier><Observable>Yes</Observable></Property><Command><Identifier>C2</Identifier></Command></Feature>",
                ["String content"] = FeatureStart + "<Identifier>  A b  </Identifier><DisplayName><![CDATA[<x>]]> &amp; <!-- c -->tail</DisplayName><Description>line1\r\nline2\rline3\nline4</Description></Feature>",
                ["Whitespace only content"] = FeatureStart + "<Identifier>   </Identifier><DisplayName/><Description></Description></Feature>",
                ["Preserved whitespace"] = FeatureStart + "<Identifier xml:space=\"preserve\">   </Identifier></Feature>",
                ["Interleaved parameters"] = FeatureStart + "<Command><Parameter><Identifier>P1</Identifier></Parameter><Response><Identifier>R1</Identifier></Response><Parameter><Identifier>P2</Identifier></Parameter><IntermediateResponse/><Observable>Yes</Observable></Command></Feature>",
                ["Defined execution errors"] = FeatureStart + "<Command><DefinedExecutionErrors><Identifier>E1</Identifier><Other>x</Other><Identifier/><Identifier> E2 </Identifier></DefinedExecutionErrors><DefinedExecutionErrors><Identifier>E3</Identifier></DefinedExecutionErrors></Command></Feature>",
                ["Empty defined execution errors"] = FeatureStart + "<Property><DefinedExecutionErrors/></Property><Metadata><DefinedExecutionErrors></DefinedExecutionErrors></Metadata></Feature>",
                ["Nil defined execution errors"] = FeatureStart + "<Property><DefinedExecutionErrors xsi:nil=\"true\"><Identifier>E1</Identifier></DefinedExecutionErrors></Property><Metadata><DefinedExecutionErrors xsi:nil=\"1\"/></Metadata><Command><DefinedExecutionErrors xsi:nil=\"false\"><Identifier>E1</Identifier></DefinedExecutionErrors></Command></Feature>",
                ["Invalid observable"] = FeatureStart + "<Command><Observable>yes</Observable></Command></Feature>",
                ["Observable with whitespace"] = FeatureStart + "<Property><Observable> Yes</Observable></Property></Feature>",
                ["Empty items"] = FeatureStart + "<Command/><Property/><Metadata/><DefinedExecutionError/><DataTypeDefinition/></Feature>",
                ["Wrong namespace items"] = "<Feature xmlns=\"http://www.sila-standard.org\"><Command xmlns=\"urn:other\"><Identifier>C</Identifier></Command><Command><Identifier xmlns=\"\">C</Identifier></Command></Feature>",
                ["Nested unknown in data type"] = FeatureStart + "<DataTypeDefinition><Identifier>T</Identifier><DataType><Foo/><Basic>String</Basic><List><DataType><Basic>Integer</Basic></DataType></List></DataType></DataTypeDefinition></Feature>",
                ["Xsi type named"] = FeatureStart + "<DataTypeDefinition xsi:type=\"SiLAElement\"><Identifier>T</Identifier><DataType xsi:type=\"DataTypeType\"><Basic>String</Basic></DataType></DataTypeDefinition></Feature>",
                ["Xsi type prefixed"] = FeatureStart + "<DataTypeDefinition xmlns:s=\"http://www.sila-standard.org\" xsi:type=\"s:SiLAElement\"><Identifier>T</Identifier></DataTypeDefinition></Feature>",
                ["Xsi type wrong"] = FeatureStart + "<DataTypeDefinition xsi:type=\"Other\"><Identifier>T</Identifier></DataTypeDefinition></Feature>",
                ["Xsi type undefined prefix"] = FeatureStart + "<DataTypeDefinition xsi:type=\"p:SiLAElement\"><Identifier>T</Identifier></DataTypeDefinition></Feature>",
                ["Xsi type anonymous"] = FeatureStart + "<Command xsi:type=\"FeatureCommand\"><Identifier>T</Identifier></Command></Feature>",
                ["Xsi type anonymous other"] = FeatureStart + "<Command xsi:type=\"Command\"><Identifier>T</Identifier></Command></Feature>",
                ["Xsi nil on element"] = FeatureStart + "<Command xsi:nil=\"true\"><Identifier>T</Identifier></Command><Property><DataType xsi:nil=\"true\"><Basic>String</Basic></DataType></Property></Feature>",
                ["Xsi nil on root"] = "<Feature xsi:nil=\"true\" xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><Identifier>A</Identifier></Feature>",
                ["Malformed"] = FeatureStart + "<Identifier>A</Feature>",
                ["Empty document"] = string.Empty,
                ["Trailing content"] = FeatureStart + "</Feature><!-- x -->",
                ["DTD"] = "<!DOCTYPE Feature [<!ENTITY e \"entity\">]>" + FeatureStart + "<Identifier>&e;</Identifier></Feature>",
            };
            return cases.Select( c => new TestCaseData( c.Value ).SetArgDisplayNames( c.Key ) );
        }

        [TestCaseSource( nameof( FeatureDocuments ) )]
        public void Feature_LoadFromXml_EqualsXmlSerializer( string xml )
        {
            AssertSameOutcome( () => OracleLoadFeature( xml ), () => FeatureSerializer.LoadFromXml( xml ) );
        }

        public static IEnumerable<TestCaseData> DataTypeDocuments()
        {
            var cases = new Dictionary<string, string>
            {
                ["Basic"] = "<DataType xmlns=\"http://www.sila-standard.org\"><Basic>Real</Basic></DataType>",
                ["Existing test"] = "<DataType xmlns=\"http://www.sila-standard.org\"><Constrained><DataType><Basic>String</Basic></DataType><Constraints><Length>0</Length></Constraints></Constrained></DataType>",
                ["Empty"] = "<DataType xmlns=\"http://www.sila-standard.org\"/>",
                ["Wrong root"] = "<DataTypeType xmlns=\"http://www.sila-standard.org\"/>",
                ["No namespace"] = "<DataType><Basic>Real</Basic></DataType>",
                ["Invalid basic"] = "<DataType xmlns=\"http://www.sila-standard.org\"><Basic>real</Basic></DataType>",
                ["Basic with whitespace"] = "<DataType xmlns=\"http://www.sila-standard.org\"><Basic>Real </Basic></DataType>",
                ["Two choices"] = "<DataType xmlns=\"http://www.sila-standard.org\"><DataTypeIdentifier>A</DataTypeIdentifier><Basic>Real</Basic><List/></DataType>",
                ["Identifier"] = "<DataType xmlns=\"http://www.sila-standard.org\"><DataTypeIdentifier> A </DataTypeIdentifier></DataType>",
                ["Empty identifier"] = "<DataType xmlns=\"http://www.sila-standard.org\"><DataTypeIdentifier/></DataType>",
                ["Prefixed"] = "<s:DataType xmlns:s=\"http://www.sila-standard.org\"><s:List><s:DataType><s:Basic>Binary</s:Basic></s:DataType></s:List></s:DataType>",
                ["Structure"] = "<DataType xmlns=\"http://www.sila-standard.org\"><Structure><Element><Identifier>A</Identifier><DataType><Basic>Integer</Basic></DataType></Element><Foo/><Element/></Structure></DataType>",
                ["Nil root"] = "<DataType xsi:nil=\"true\" xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><Basic>Real</Basic></DataType>",
                ["Nil root wrong type"] = "<DataType xsi:nil=\"true\" xsi:type=\"Foo\" xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"/>",
                ["Null root 1999"] = "<DataType xsi:null=\"1\" xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/1999/XMLSchema-instance\"/>",
                ["Invalid nil value"] = "<DataType xsi:nil=\"maybe\" xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"/>",
                ["Root xsi type"] = "<DataType xsi:type=\"DataTypeType\" xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><Basic>Real</Basic></DataType>",
                ["Root xsi type wrong"] = "<DataType xsi:type=\"ListType\" xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><Basic>Real</Basic></DataType>",
                ["Root xsi type 2000"] = "<DataType xsi:type=\"Other\" xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2000/10/XMLSchema-instance\"><Basic>Real</Basic></DataType>",
                ["Constrained xsi type"] = "<DataType xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><Constrained xsi:type=\"ConstrainedType\"><Constraints xsi:type=\"Constraints\"><Unit xsi:type=\"ConstraintsUnit\"/></Constraints></Constrained></DataType>",
                ["List xsi type wrong"] = "<DataType xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><List xsi:type=\"StructureType\"/></DataType>",
            };
            return cases.Select( c => new TestCaseData( c.Value ).SetArgDisplayNames( c.Key ) );
        }

        [TestCaseSource( nameof( DataTypeDocuments ) )]
        public void DataType_LoadType_EqualsXmlSerializer( string xml )
        {
            AssertSameOutcome( () => OracleLoadDataType( xml ), () => DataTypeSerializer.LoadType( xml ) );
        }

        public static IEnumerable<TestCaseData> ConstraintsDocuments()
        {
                        var cases = new Dictionary<string, string>
            {
                ["Empty"] = string.Empty,
                ["Integers with whitespace"] = "<Length> 5 </Length><MinimalLength>\r\n1\t</MinimalLength><MaximalLength>x</MaximalLength><ElementCount> 2</ElementCount><MinimalElementCount>3 </MinimalElementCount><MaximalElementCount/>",
                ["Strings with whitespace"] = "<Pattern> a </Pattern><MaximalExclusive> 1 </MaximalExclusive><MaximalInclusive>2</MaximalInclusive><MinimalExclusive>3</MinimalExclusive><MinimalInclusive>4</MinimalInclusive>",
                ["Set"] = "<Set><Value>A</Value><Value> B </Value><Value/><Other/></Set>",
                ["Empty set"] = "<Set/><AllowedTypes></AllowedTypes>",
                ["Nil set"] = "<Set xsi:nil=\"true\"/><AllowedTypes xsi:nil=\"true\"><DataType><Basic>String</Basic></DataType></AllowedTypes>",
                ["Duplicate set"] = "<Set><Value>A</Value></Set><Set><Value>B</Value></Set><Length>1</Length><Length>2</Length>",
                ["Unit"] = "<Unit><Label>m</Label><Factor> 1.5E0 </Factor><Offset>-0.25</Offset><UnitComponent><SIUnit>Meter</SIUnit><Exponent> -2 </Exponent></UnitComponent><UnitComponent/><UnitComponent><SIUnit>Candela</SIUnit></UnitComponent></Unit>",
                ["Unit decimals"] = "<Unit><Factor>1.5</Factor><Offset> +3 </Offset></Unit>",
                ["Unit invalid decimal"] = "<Unit><Factor>1,5</Factor></Unit>",
                ["Unit empty decimal"] = "<Unit><Factor/></Unit>",
                ["Unit invalid SI unit"] = "<Unit><UnitComponent><SIUnit>Foot</SIUnit></UnitComponent></Unit>",
                ["Content type"] = "<ContentType><Type>application</Type><Subtype>json</Subtype><Parameters><Parameter><Attribute>charset</Attribute><Value>utf-8</Value></Parameter><Parameter/><Foo/></Parameters></ContentType>",
                ["Content type nil parameters"] = "<ContentType><Parameters xsi:nil=\"true\"/></ContentType>",
                ["Fully qualified identifier"] = "<FullyQualifiedIdentifier>CommandParameterIdentifier</FullyQualifiedIdentifier>",
                ["Invalid fully qualified identifier"] = "<FullyQualifiedIdentifier>Command</FullyQualifiedIdentifier>",
                ["Schema url"] = "<Schema><Type>Json</Type><Url>http://example.org</Url></Schema>",
                ["Schema inline"] = "<Schema><Inline><![CDATA[<schema/>]]></Inline><Type>Xml</Type></Schema>",
                ["Schema both"] = "<Schema><Inline>a</Inline><Url>b</Url></Schema>",
                ["Schema invalid type"] = "<Schema><Type>Yaml</Type></Schema>",
                ["Allowed types"] = "<AllowedTypes><DataType><Basic>Integer</Basic></DataType><Other/><DataType><List><DataType><DataTypeIdentifier>X</DataTypeIdentifier></DataType></List></DataType><DataType/></AllowedTypes>",
                ["Nested constrained"] = "<AllowedTypes><DataType><Constrained><DataType><Basic>Real</Basic></DataType><Constraints><Unit><Label>s</Label><Factor>1</Factor><Offset>0</Offset></Unit></Constraints></Constrained></DataType></AllowedTypes>",
                ["Unknown"] = "<Foo/><Bar>text</Bar>text<Length>3</Length>",
            };
            return cases.Select( c => new TestCaseData( c.Value ).SetArgDisplayNames( c.Key ) );
        }

        /// <summary>
        /// Tests the content of a Constraints element as a ConstraintSerializer document, where only the children are in the SiLA namespace,
        /// and as part of a constrained data type
        /// </summary>
        [TestCaseSource( nameof( ConstraintsDocuments ) )]
        public void Constraints_LoadConstraints_EqualsXmlSerializer( string content )
        {
            var prefixedContent = Regex.Replace( content, "<(/?)([A-Za-z])", "<$1s:$2" );
            var constraintsXml = "<Constraints xmlns:s=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" + prefixedContent + "</Constraints>";
            var dataTypeXml = "<DataType xmlns=\"http://www.sila-standard.org\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><Constrained><DataType><Basic>String</Basic></DataType><Constraints>" + content + "</Constraints></Constrained></DataType>";
            AssertSameOutcome( () => OracleLoadConstraints( constraintsXml ), () => ConstraintSerializer.LoadConstraints( constraintsXml ) );
            AssertSameOutcome( () => OracleLoadDataType( dataTypeXml ), () => DataTypeSerializer.LoadType( dataTypeXml ) );
        }

        [TestCase( "<Constraints xmlns=\"http://www.sila-standard.org\"><Length>1</Length></Constraints>" )]
        [TestCase( "<Constraints><Length>1</Length><Length xmlns=\"http://www.sila-standard.org\">2</Length></Constraints>" )]
        [TestCase( "<Constraints xsi:nil=\"true\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" />" )]
        [TestCase( "<Constrained xmlns=\"http://www.sila-standard.org\"/>" )]
        public void Constraints_LoadConstraintsDocument_EqualsXmlSerializer( string xml )
        {
            AssertSameOutcome( () => OracleLoadConstraints( xml ), () => ConstraintSerializer.LoadConstraints( xml ) );
        }

        [Test]
        public void Feature_LoadFromXml_SkipsLeadingGarbageLikeBefore()
        {
            var xml = "\uFEFFgarbage" + FeatureStart + "<Identifier>A</Identifier></Feature>";
            Assert.That( FeatureSerializer.LoadFromXml( xml ).Identifier, Is.EqualTo( "A" ) );
        }

        [Test]
        public void Feature_LoadFromPath_FallsBackForLeadingGarbage()
        {
            var path = Path.Combine( TestContext.CurrentContext.WorkDirectory, Guid.NewGuid() + ".sila.xml" );
            try
            {
                File.WriteAllText( path, "garbage" + FeatureStart + "<Identifier>A</Identifier></Feature>" );
                Assert.That( FeatureSerializer.Load( path ).Identifier, Is.EqualTo( "A" ) );
            }
            finally
            {
                File.Delete( path );
            }
        }

        [Test]
        public void Feature_LoadFromXml_ReportsErrorsAsInvalidOperationException()
        {
            var exception = Assert.Throws<InvalidOperationException>( () => FeatureSerializer.LoadFromXml( FeatureStart + "<Command><Observable>Maybe</Observable></Command></Feature>" ) );
            Assert.That( exception.Message, Does.Contain( "(1," ) );
            Assert.That( exception.InnerException, Is.InstanceOf<InvalidOperationException>() );
            Assert.That( exception.InnerException.Message, Does.Contain( "Maybe" ) );
        }

        #endregion
    }
}
