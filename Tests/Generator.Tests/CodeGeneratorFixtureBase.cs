using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using NUnit.Framework;
using Tecan.Sila2.Generator.Generators;

namespace Tecan.Sila2.Generator.Test
{
    public abstract class CodeGeneratorFixtureBase
    {
        private static readonly CSharpCodeProvider cSharp = new CSharpCodeProvider();
        private static readonly SHA1Managed Sha = new SHA1Managed();

        [SetUp]
        public void SetCurrentDirectory()
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        protected abstract string Name { get; }

        protected string FeatureDefinitionFile => Path.Combine(Name , "Feature.xml");

        protected abstract Type Interface { get; }
        protected string InterfaceReference => Path.Combine(Name , "ServiceReference.cs");
        protected string DtoReference => Path.Combine(Name, "DtoReference.cs");
        protected string ProviderReference => Path.Combine(Name, "ProviderReference.cs");

        [Test]
        public void Test_GenerateFeature_SilaFeature()
        {
            var generator = new FeatureDefinitionGenerator(null, null);

            var feature = generator.GenerateFeature(Interface);
            var xmlCode = FeatureSerializer.SaveToString(feature);
            var path = Path.GetTempFileName();
            File.WriteAllText(path, xmlCode);
            try
            {
                Assert.That( GetCanonicalRepresentation( path ), Is.EqualTo( GetCanonicalRepresentation( FeatureDefinitionFile ) ) );
            }
            finally
            {
                File.Delete(path);
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
        public void Test_GenerateInterface_SilaFeature()
        {
            var feature = FeatureSerializer.Load(FeatureDefinitionFile);
            var interfaceGenerator = new InterfaceGenerator();
            var unit = interfaceGenerator.GenerateInterfaceUnit(feature,
                "Tecan.Sila2.Generator.Test.TestReference" );

            GenerateAndCompare(unit, InterfaceReference);
        }

        [Test]
        public void Test_GenerateDto_SilaFeature()
        {
            var feature = FeatureSerializer.Load(FeatureDefinitionFile);
            var dtoGenerator = new DtoGenerator(new CodeNameProvider());
            var unit = dtoGenerator.GenerateInterfaceUnit(feature,
                "Tecan.Sila2.Generator.Test.TestReference" );

            GenerateAndCompare(unit, DtoReference);
        }

        [Test]
        public void Test_GenerateProvider_SilaFeature()
        {
            var feature = FeatureSerializer.Load(FeatureDefinitionFile);
            var nameProvider = new CodeNameProvider();
            var providerGenerator = new ProviderGenerator(nameProvider, new ClientGenerator(nameProvider, null), new ServerGenerator(nameProvider, null));
            var unit = providerGenerator.GenerateInterfaceUnit(feature,
                "Tecan.Sila2.Generator.Test.TestReference",
                generateServer: true,
                generateClient: true);

            GenerateAndCompare(unit, ProviderReference);
        }

        private static void GenerateAndCompare(CodeCompileUnit unit, string referenceFile)
        {
            var options = new CodeGeneratorOptions
            {
                BlankLinesBetweenMembers = true,
                BracingStyle = "C",
                ElseOnClosing = true,
                IndentString = "    ",
                VerbatimOrder = false
            };
            using (var writer = new StringWriter())
            {
                cSharp.GenerateCodeFromCompileUnit(unit, writer, options);

                var generatedCode = writer.ToString();
                AssertEqualIgnoreWhitespace(File.ReadAllText(referenceFile), generatedCode);
            }
        }

        private static void AssertEqualIgnoreWhitespace(string reference, string actual)
        {
            Assert.AreEqual(EliminateWhitespaces(reference), EliminateWhitespaces(actual));
        }

        private static string EliminateWhitespaces(string input)
        {
            return Regex.Replace( Regex.Replace( input, "//[^/].*", string.Empty ), @"\s+", string.Empty );
        }
    }
}
