using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Generators;
using Tecan.Sila2.Generator.Test.TestReference;

namespace Tecan.Sila2.Generator.Test.Centrifuge
{
    [TestFixture]
    public class CentrifugeFixture : CodeGeneratorFixtureBase
    {
        protected override string Name => "Centrifuge";

        protected override Type Interface => typeof( ICentrifuge );

        protected override ICodeNameProvider CreateNameProvider()
        {
            var nameProvider = new CodeNameProvider();
            nameProvider.RegisterDifferentType( nameof( ICentrifuge.Speed ), typeof( int ) );
            nameProvider.RegisterDifferentType( "RunCrazy.Speeds.Item", typeof( int ) );
            nameProvider.RegisterDifferentType( "RunCrazy.Speeds", typeof( ICollection<int> ) );
            return nameProvider;
        }
    }
}
