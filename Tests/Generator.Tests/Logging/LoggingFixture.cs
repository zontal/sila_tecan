using System;
using NUnit.Framework;

namespace Tecan.Sila2.Generator.Test.Logging
{
    [TestFixture]
    public class LoggingFixture : CodeGeneratorFixtureBase
    {
        protected override string Name => "Logging";

        protected override Type Interface => typeof(ILoggingService);
    }
}
