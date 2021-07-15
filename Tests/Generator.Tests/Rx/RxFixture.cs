using System;
using NUnit.Framework;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Generators;
using Tecan.Sila2.Generator.Test.TestReference;

namespace Tecan.Sila2.Generator.Test.Rx
{
    [TestFixture]
    public class RxFixture : CodeGeneratorFixtureBase
    {
        protected override string Name => "Rx";

        protected override Type Interface => typeof(IRxService);

        protected override ICodeNameProvider CreateNameProvider()
        {
            var registry = new CodeNameProvider();
            registry.RegisterMethod( nameof( IRxService.RunRxObservable ),
                typeof( IRxService ).GetMethod( nameof( IRxService.RunRxObservable ) ) );
            return registry;
        }
    }
}
