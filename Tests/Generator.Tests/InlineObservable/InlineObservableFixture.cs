using System;
using NUnit.Framework;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Generators;
using Tecan.Sila2.Generator.Test.TestReference;

namespace Tecan.Sila2.Generator.Test.InlineObservable
{
    [TestFixture]
    public class InlineObservableFixture : CodeGeneratorFixtureBase
    {
        protected override string Name => "InlineObservable";

        protected override Type Interface => typeof(IInlineObservableService);

        protected override ICodeNameProvider CreateNameProvider()
        {
            var registry = new CodeNameProvider();
            registry.RegisterMethod( nameof(IInlineObservableService.TestInlineObservable),
                typeof(IInlineObservableService).GetMethod( nameof(IInlineObservableService.TestInlineObservable) ) );
            return registry;
        }
    }
}
