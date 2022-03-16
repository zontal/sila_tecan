using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Generators;
using Tecan.Sila2.Generator.Helper;
using Tecan.Sila2.Generator.Test.TestReference;

namespace Tecan.Sila2.Generator.Test.Configuration
{
    [TestFixture]
    public class ConfiguredServiceFixture : CodeGeneratorFixtureBase
    {
        protected override string Name => "Configuration";

        protected override Type Interface => typeof( IConfiguredService );

        protected override ICodeNameProvider CreateNameProvider()
        {
            var nameProvider = new CodeNameProvider();
            var mock = new Mock();
            var speedFn = new Func<Speed>( mock.GetSpeed );
            nameProvider.RegisterMethod( "Speed", speedFn.Method );
            nameProvider.RegisterDifferentType( "Speed", typeof( Speed ) );
            nameProvider.RegisterDifferentType( "AccelerateTo.Target", typeof( Speed ) );
            return nameProvider;
        }

        private class Mock : IConfiguredService
        {
            public void AccelerateTo( Speed target )
            {
                throw new NotImplementedException();
            }

            public Speed GetSpeed()
            {
                throw new NotImplementedException();
            }
        }

        protected override FeatureGenerationConfig FeatureGeneratorConfig => FeatureDefinitionConfigHelper.LoadConfigMappingFile( Path.Combine( Name, "ConfiguredService.xml" ) );
    }
}
