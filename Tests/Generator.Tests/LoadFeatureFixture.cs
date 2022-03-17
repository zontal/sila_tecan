using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Generator.Test
{
    [TestFixture]
    public class LoadFeatureFixture
    {
        [Test]
        public void LoadSimulationController_Succeeds()
        {
            Assert.DoesNotThrow( () => FeatureSerializer.Load( "SimulationController.sila.xml" ) );
        }

        [Test]
        public void LoadSimulationControllerFromString_Succeeds()
        {
            var ch = '\ufeff';
            var text = File.ReadAllText( "SimulationController.sila.xml" );

            Assert.DoesNotThrow( () => FeatureSerializer.LoadFromXml( text ) );
        }

        [SetUp]
        public void SetCurrentDirectory()
        {
            Environment.CurrentDirectory = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
        }
    }
}
