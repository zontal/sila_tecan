using System;
using NUnit.Framework;

namespace Tecan.Sila2.Generator.Test.TemperatureController
{
    [TestFixture]
    public class TemperatureControllerFixture : CodeGeneratorFixtureBase
    {
        protected override string Name => "TemperatureController";

        protected override Type Interface => typeof(ITemperatureController);
    }
}
