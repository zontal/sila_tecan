using System;
using System.IO;
using NUnit.Framework;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

namespace Tecan.Sila2.Generator.Test.AmbiguityResolver
{
    [TestFixture]
    public class AmbiguityResolverFixture : CodeGeneratorFixtureBase
    {
        protected override string Name => "AmbiguityResolver";

        protected override Type Interface => typeof( IAmbiguityResolverService );

        protected override FeatureGenerationConfig FeatureGeneratorConfig => FeatureDefinitionConfigHelper.LoadConfigMappingFile( Path.Combine( Name, "AmbiguityResolver.xml" ) );
    }
}
