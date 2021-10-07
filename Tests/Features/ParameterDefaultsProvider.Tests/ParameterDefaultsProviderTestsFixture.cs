using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.ParameterConstraintsProvider;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.ParameterDefaultsProvider.Tests
{
    [TestFixture]
    public class ParameterDefaultsProviderTestsFixture
    {
        private ParameterConstraintsProviderService _constraintsProvider;
        private Mock<ISiLAServer> _serverMock;

        [SetUp]
        public void Initialize()
        {
            var setProviderResolver = new SetProviderResolver();
            var fileNameResolver = new FileConstraintResolver();
            _serverMock = new Mock<ISiLAServer>(MockBehavior.Strict);
            _constraintsProvider = new ParameterConstraintsProviderService(
                new Lazy<ISiLAServer>( () => _serverMock.Object ),
                new IParameterConstraintResolver[] { setProviderResolver, fileNameResolver } );
        }

        public static IEnumerable<string> GetPossibleParameters()
        {
            yield return "Foo";
            yield return "Bar";
        }

        [Test]
        public void GetParameterDefaults_IdentifiesStaticMethodParameterConstraint()
        {
            var exampleMock = new Mock<IExampleSetProviderService>();
            var provider = new ExampleSetProviderServiceProvider(exampleMock.Object, _serverMock.Object);
            _serverMock.Setup( s => s.Features ).Returns( new List<IFeatureProvider>() { provider } );
            var constraints = _constraintsProvider.ParametersConstraints;
            Assert.That(constraints, Is.Not.Empty);
            Assert.That(constraints.Count, Is.EqualTo(1));
            var constraint = constraints.Single();
            
            Assert.That(constraint.CommandParameterIdentifier, Is.EqualTo( "tecan.sila.parameterdefaultsprovider/tests/ExampleSetProviderService/v1/Command/ExampleMethod/Parameter/parameter" ) );
            var constraintParsed = ConstraintSerializer.LoadConstraints( constraint.Constraints );
            Assert.That( constraintParsed.Set, Is.Not.Null);
            Assert.That( constraintParsed.Set.Length, Is.EqualTo(2));
            var set = constraintParsed.Set;
            Assert.That(set[0], Is.EqualTo("Foo"));
            Assert.That(set[1], Is.EqualTo("Bar"));
        }

        [Test]
        public void GetParameterDefaults_IdentifiesFileConstraint()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var exampleMock = new Mock<IExampleFileConstraintProviderService>();
            var provider = new ExampleFileConstraintProviderServiceProvider( exampleMock.Object, _serverMock.Object );
            _serverMock.Setup( s => s.Features ).Returns( new List<IFeatureProvider>() { provider } );
            var constraints = _constraintsProvider.ParametersConstraints;
            Assert.That( constraints, Is.Not.Empty );
            Assert.That( constraints.Count, Is.EqualTo( 1 ) );
            var constraint = constraints.Single();

            Assert.That( constraint.CommandParameterIdentifier, Is.EqualTo( "tecan.sila.parameterdefaultsprovider/tests/ExampleFileConstraintProviderService/v1/Command/FileExampleMethod/Parameter/parameter" ) );
            var constraintParsed = ConstraintSerializer.LoadConstraints( constraint.Constraints );
            Assert.That( constraintParsed.Set, Is.Not.Null );
            Assert.That( constraintParsed.Set.Length, Is.EqualTo( 2 ) );
            var set = constraintParsed.Set;
            Assert.That( set, Contains.Item("Foo.txt") );
            Assert.That( set, Contains.Item( "Bar.txt" ) );
        }

        [Test]
        public void GetParameterDefaults_NoConstraints_NoConstraint()
        {
            var exampleMock = new Mock<ISiLAService>();
            var provider = new SiLAServiceProvider(exampleMock.Object, _serverMock.Object);
            _serverMock.Setup( s => s.Features ).Returns( new List<IFeatureProvider>() { provider } );
            var constraints = _constraintsProvider.ParametersConstraints;
            Assert.That(constraints, Is.Null.Or.Empty);
        }
    }
}
