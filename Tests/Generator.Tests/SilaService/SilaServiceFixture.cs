using System;
using NUnit.Framework;

namespace Tecan.Sila2.Generator.Test.SilaService
{
    [TestFixture]
    public class SilaServiceFixture : CodeGeneratorFixtureBase
    {
        protected override string Name => "SilaService";

        protected override Type Interface => typeof(ISiLAService);
    }
}
