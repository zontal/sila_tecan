using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.Tests;

namespace Tecan.Sila2.IntegrationTests
{
    // Integration tests do not work on CI server, unfortunately
    [TestFixture( Category = "IntegrationTests" )]
    public class ClientInitiatedIntegrationTestFixture : IntegrationTestFixtureBase
    {
    }
}
