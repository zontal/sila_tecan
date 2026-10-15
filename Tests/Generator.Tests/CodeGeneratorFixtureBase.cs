using System;
using System.CodeDom;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Generators;
using Tecan.Sila2.Generator.Helper;
using Tecan.Sila2.Generator.TypeTranslation;
using Tecan.Sila2.Generator.Validation;

namespace Tecan.Sila2.Generator.Test
{
    public abstract class CodeGeneratorFixtureBase
    {
        private ITypeTranslationProvider _typeTranslationProvider;
        private IAmbiguityResolver _ambiguityResolver;
        private ICodeNameRegistry _codeNameRegistry;

        [SetUp]
        public void SetCurrentDirectory()
        {
            Environment.CurrentDirectory = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
            _codeNameRegistry = new CodeNameProvider();
            _typeTranslationProvider = new TypeTranslationProvider( new ITypeTranslator[]
            {
                new BasicTypeTranslator( _codeNameRegistry ),
                new CollectionTranslator( _codeNameRegistry ),
                new DictionaryTranslator( _codeNameRegistry ),
                new ConstraintsTranslator(),
                new GeneratedTypeTranslator()
            } );
            _ambiguityResolver = new Generators.AmbiguityResolver();
        }

        protected abstract string Name
        {
            get;
        }

        protected string FeatureDefinitionFile => Path.Combine( Name, "Feature.xml" );

        protected abstract Type Interface
        {
            get;
        }

        protected string InterfaceReference => Path.Combine( Name, "ServiceReference.cs" );
        protected string DtoReference => Path.Combine( Name, "DtoReference.cs" );
        protected string ProviderReference => Path.Combine( Name, "ProviderReference.cs" );
        protected string ClientReference => Path.Combine( Name, "ClientReference.cs" );

        protected string GeneratedTypeModelDtoReference => Path.Combine( Name, GeneratedTypeModelFolder, "DtoReference.cs" );
        protected string GeneratedTypeModelProviderReference => Path.Combine( Name, GeneratedTypeModelFolder, "ProviderReference.cs" );
        protected string GeneratedTypeModelClientReference => Path.Combine( Name, GeneratedTypeModelFolder, "ClientReference.cs" );

        private const string ReferenceNamespace = "Tecan.Sila2.Generator.Test.TestReference";
        private const string GeneratedTypeModelFolder = "GeneratedTypeModel";
        private const string GeneratedTypeModelNamespace = ReferenceNamespace + "." + GeneratedTypeModelFolder;

        protected virtual FeatureGenerationConfig FeatureGeneratorConfig => null;

        protected virtual IGeneratorConfigSource CreateConfigSource()
        {
            var configSource = new GeneratorConfigSource();
            var config = FeatureGeneratorConfig;
            if(config != null)
            {
                configSource.Add( config );
            }
            return configSource;
        }

        [Test]
        public virtual void Test_GenerateFeature_SilaFeature()
        {
            var generator = new FeatureDefinitionGenerator( _codeNameRegistry, _typeTranslationProvider, CreateConfigSource(), null, _ambiguityResolver );

            var feature = generator.GenerateFeature( Interface );
            var xmlCode = FeatureSerializer.SaveToString( feature );
            var path = Path.GetTempFileName();
            File.WriteAllText( path, xmlCode );
            try
            {
                Assert.That( GetCanonicalRepresentation( path ), Is.EqualTo( GetCanonicalRepresentation( FeatureDefinitionFile ) ) );
            }
            finally
            {
                File.Delete( path );
            }
        }

        private string GetCanonicalRepresentation( string path )
        {
            var transformedReference = new XmlDsigC14NTransform( false );
            using(var reference = File.OpenRead( path ))
            {
                transformedReference.LoadInput( reference );
            }

            return Encoding.UTF8.GetString( ((MemoryStream)transformedReference.GetOutput()).ToArray() );
        }

        [Test]
        public virtual void Test_GenerateInterface()
        {
            var feature = FeatureSerializer.Load( FeatureDefinitionFile );
            var interfaceGenerator = new InterfaceGenerator( _typeTranslationProvider, null, CreateConfigSource() );
            var unit = interfaceGenerator.GenerateInterfaceUnit( feature,
                "Tecan.Sila2.Generator.Test.TestReference" );

            GenerateAndCompare( unit, InterfaceReference );
        }

        [Test]
        public virtual void Test_GenerateDto()
        {
            GenerateAndCompare( GenerateDto, ReferenceNamespace, false, DtoReference );
        }

        [Test]
        public virtual void Test_GenerateDto_GeneratedTypeModel()
        {
            GenerateAndCompare( GenerateDto, GeneratedTypeModelNamespace, true, GeneratedTypeModelDtoReference );
        }

        private CodeCompileUnit GenerateDto( Feature feature, string nameSpace )
        {
            var dtoGenerator = new DtoGenerator( _typeTranslationProvider, CreateNameProvider(), CreateConfigSource(), new IValidationCreator[]
            {
                new IdentifierValidation(),
                new LengthValidation(),
                new NumberValidation(),
                new RegexValidationCreator()
            } );
            return dtoGenerator.GenerateInterfaceUnit( feature, nameSpace );
        }

        protected virtual ICodeNameProvider CreateNameProvider()
        {
            return new CodeNameProvider();
        }

        [Test]
        public virtual void Test_GenerateProvider()
        {
            GenerateAndCompare( GenerateProvider, ReferenceNamespace, false, ProviderReference );
        }

        [Test]
        public virtual void Test_GenerateProvider_GeneratedTypeModel()
        {
            GenerateAndCompare( GenerateProvider, GeneratedTypeModelNamespace, true, GeneratedTypeModelProviderReference );
        }

        private CodeCompileUnit GenerateProvider( Feature feature, string nameSpace )
        {
            var providerGenerator = new ServerGenerator( _typeTranslationProvider, CreateNameProvider(), new MefGenerator(), null, CreateConfigSource() );
            return providerGenerator.GenerateServer( feature, nameSpace );
        }

        [Test]
        public virtual void Test_GenerateClient()
        {
            GenerateAndCompare( GenerateClient, ReferenceNamespace, false, ClientReference );
        }

        [Test]
        public virtual void Test_GenerateClient_GeneratedTypeModel()
        {
            GenerateAndCompare( GenerateClient, GeneratedTypeModelNamespace, true, GeneratedTypeModelClientReference );
        }

        private CodeCompileUnit GenerateClient( Feature feature, string nameSpace )
        {
            var clientGenerator = new ClientGenerator( CreateNameProvider(), _typeTranslationProvider, null, new MefGenerator(), CreateConfigSource() );
            return clientGenerator.GenerateClient( feature, nameSpace );
        }

        private void GenerateAndCompare( Func<Feature, string, CodeCompileUnit> generate, string nameSpace, bool useGeneratedTypeModel, string referenceFile )
        {
            var feature = FeatureSerializer.Load( FeatureDefinitionFile );
            var previous = GeneratorSettings.UseGeneratedTypeModel;
            GeneratorSettings.UseGeneratedTypeModel = useGeneratedTypeModel;
            try
            {
                GenerateAndCompare( generate( feature, nameSpace ), referenceFile );
            }
            finally
            {
                GeneratorSettings.UseGeneratedTypeModel = previous;
            }
        }

        private static void GenerateAndCompare( CodeCompileUnit unit, string referenceFile )
        {
            var generatedCode = CodeGenerationHelper.GenerateCSharpCode( unit );
            AssertEqualIgnoreWhitespaceAndParentheses( File.ReadAllText( referenceFile ), generatedCode );
        }

        private static void AssertEqualIgnoreWhitespaceAndParentheses( string reference, string actual )
        {
            Assert.AreEqual( EliminateParentheses( EliminateWhitespaces( reference ) ), EliminateParentheses( EliminateWhitespaces( actual ) ) );
        }

        private static string EliminateWhitespaces( string input )
        {
            return Regex.Replace( Regex.Replace( input, "//[^/].*", string.Empty ), @"\s+", string.Empty );
        }

        private static string EliminateParentheses( string input )
        {
            return input.Replace( "(", string.Empty ).Replace( ")", string.Empty ).Replace("\"+\"\"", "\"");
        }
    }
}