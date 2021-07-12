using System;
using NUnit.Framework;

namespace Tecan.Sila2.Generator.Test.Empty
{
    [TestFixture]
    public class EmptyFixture : CodeGeneratorFixtureBase
    {
        protected override string Name => "Empty";

        protected override Type Interface => typeof( IEmptyService );
    }
}
