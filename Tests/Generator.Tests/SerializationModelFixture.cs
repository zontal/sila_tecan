using System.Linq;
using NUnit.Framework;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Generators;
using Tecan.Sila2.Generator.Helper;
using Tecan.Sila2.Generator.TypeTranslation;
using Tecan.Sila2.Generator.Validation;

namespace Tecan.Sila2.Generator.Test
{
    [TestFixture]
    public class SerializationModelFixture
    {
        private const string Namespace = "Tecan.Sila2.Generator.Test.TestReference";

        // a list of strings and a data type named ListOfString lead to the same serializer base name
        private const string CollidingFeature = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Feature xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" SiLA2Version=""1.0"" FeatureVersion=""1.0"" Originator=""tecan.sila.generator.test"" Category=""test"" xmlns=""http://www.sila-standard.org"">
  <Identifier>CollisionService</Identifier>
  <DisplayName>Collision Service</DisplayName>
  <Description />
  <Property>
    <Identifier>Names</Identifier>
    <DisplayName>Names</DisplayName>
    <Description />
    <Observable>No</Observable>
    <DataType>
      <List>
        <DataType>
          <Basic>String</Basic>
        </DataType>
      </List>
    </DataType>
  </Property>
  <Property>
    <Identifier>Aliases</Identifier>
    <DisplayName>Aliases</DisplayName>
    <Description />
    <Observable>Yes</Observable>
    <DataType>
      <DataTypeIdentifier>ListOfString</DataTypeIdentifier>
    </DataType>
  </Property>
  <DataTypeDefinition>
    <Identifier>ListOfString</Identifier>
    <DisplayName>List Of String</DisplayName>
    <Description />
    <DataType>
      <Basic>String</Basic>
    </DataType>
  </DataTypeDefinition>
</Feature>";

        private ITypeTranslationProvider _typeTranslationProvider;
        private ICodeNameProvider _nameProvider;
        private Feature _feature;

        [SetUp]
        public void SetUp()
        {
            var codeNameRegistry = new CodeNameProvider();
            _typeTranslationProvider = new TypeTranslationProvider( new ITypeTranslator[]
            {
                new BasicTypeTranslator( codeNameRegistry ),
                new CollectionTranslator( codeNameRegistry ),
                new DictionaryTranslator( codeNameRegistry ),
                new ConstraintsTranslator(),
                new GeneratedTypeTranslator()
            } );
            _nameProvider = new CodeNameProvider();
            _feature = FeatureSerializer.LoadFromXml( CollidingFeature );
        }

        [Test]
        public void GetSerializers_DistinctTypesWithSameBaseName_GetDistinctNames()
        {
            var serializers = SerializationModelHelper.GetSerializers( _feature, _nameProvider, _typeTranslationProvider );

            Assert.That( serializers.Select( s => s.Value ), Is.EqualTo( new[] { "PropertyResponseOfListOfStringDto", "PropertyResponseOfListOfStringDto2" } ) );
            Assert.That( serializers[0].Key.TypeArguments[0].BaseType, Does.StartWith( "System.Collections.Generic.List" ) );
            Assert.That( serializers[1].Key.TypeArguments[0].BaseType, Is.EqualTo( "ListOfStringDto" ) );
        }

        [Test]
        public void GeneratedCode_ReferencesSerializerOfMatchingType()
        {
            var dtoGenerator = new DtoGenerator( _typeTranslationProvider, _nameProvider, new GeneratorConfigSource(), new IValidationCreator[0] );
            var dtoCode = CodeGenerationHelper.GenerateCSharpCode( dtoGenerator.GenerateInterfaceUnit( _feature, Namespace ) );
            var providerCode = CodeGenerationHelper.GenerateCSharpCode( new ServerGenerator( _typeTranslationProvider, _nameProvider, new MefGenerator(), null, new GeneratorConfigSource() ).GenerateServer( _feature, Namespace ) );
            var clientCode = CodeGenerationHelper.GenerateCSharpCode( new ClientGenerator( _nameProvider, _typeTranslationProvider, null, new MefGenerator(), new GeneratorConfigSource() ).GenerateClient( _feature, Namespace ) );

            Assert.That( dtoCode, Does.Contain( "ProtoSerializableAttribute(typeof(PropertyResponse<System.Collections.Generic.List<Tecan.Sila2.StringDto>>))" ) );
            Assert.That( dtoCode, Does.Contain( "ProtoSerializableAttribute(typeof(PropertyResponse<ListOfStringDto>))" ) );
            Assert.That( dtoCode, Does.Contain( "public static ByteSerializer<PropertyResponse<System.Collections.Generic.List<Tecan.Sila2.StringDto>>> PropertyResponseOfListOfStringDto" ) );
            Assert.That( dtoCode, Does.Contain( "public static ByteSerializer<PropertyResponse<ListOfStringDto>> PropertyResponseOfListOfStringDto2" ) );
            Assert.That( providerCode, Does.Contain( "\"Names\", GetNames, CollisionServiceSerializers.PropertyResponseOfListOfStringDto)" ) );
            Assert.That( providerCode, Does.Contain( "\"Aliases\", GetAliases, _implementation, CollisionServiceSerializers.PropertyResponseOfListOfStringDto2)" ) );
            Assert.That( clientCode, Does.Contain( "callInfo, CollisionServiceSerializers.PropertyResponseOfListOfStringDto)" ) );
            Assert.That( clientCode, Does.Contain( "CollisionServiceSerializers.PropertyResponseOfListOfStringDto2, _cancellationTokenSource.Token)" ) );
        }
    }
}
