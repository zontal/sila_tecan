using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.ParameterConstraintsProvider;
using Tecan.Sila2.Server;
using Tecan.Sila2.Tests;

namespace Tecan.Sila2.ParameterDefaultsProvider.Tests
{
    [TestFixture]
    public class ParameterDefaultsProviderTestsFixture
    {
        private ParameterConstraintsProvider.ParameterConstraintsProvider _constraintsProvider;
        private Mock<ISiLAServer> _serverMock;

        [SetUp]
        public void Initialize()
        {
            var testLogging = new TestLoggingFactory();
            var setProviderResolver = new SetProviderResolver();
            var fileNameResolver = new FileConstraintResolver();
            _serverMock = new Mock<ISiLAServer>(MockBehavior.Strict);
            _constraintsProvider = new ParameterConstraintsProvider.ParameterConstraintsProvider(
                new Lazy<ISiLAServer>( () => _serverMock.Object ),
                new IParameterConstraintResolver[] { setProviderResolver, fileNameResolver },
                testLogging );
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
            var provider = new ExampleSetProviderServiceProvider.Server(exampleMock.Object, _serverMock.Object);
            _serverMock.Setup( s => s.Features ).Returns( new List<IFeatureProvider>() { provider } );
            var constraints = _constraintsProvider.ParametersConstraints;
            Assert.That(constraints, Is.Not.Empty);
            Assert.That(constraints.Count, Is.EqualTo(1));
            var constraint = constraints.Single();
            
            Assert.That(constraint.CommandParameterIdentifier, Is.EqualTo( "tecan.sila2.parameterdefaultsprovider/tests/ExampleSetProviderService/v1/Command/ExampleMethod/Parameter/parameter" ) );
            var dataType = DataTypeSerializer.LoadType( constraint.Constraints );
            Assert.That(dataType.Item, Is.InstanceOf<ConstrainedType>());
            var constrained = (ConstrainedType)dataType.Item;
            Assert.That(constrained.DataType.Item, Is.EqualTo(BasicType.String));
            Assert.That(constrained.Constraints.Set, Is.Not.Null);
            Assert.That(constrained.Constraints.Set.Length, Is.EqualTo(2));
            var set = constrained.Constraints.Set;
            Assert.That(set[0], Is.EqualTo("Foo"));
            Assert.That(set[1], Is.EqualTo("Bar"));
        }

        [Test]
        public void GetParameterDefaults_IdentifiesFileConstraint()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var exampleMock = new Mock<IExampleFileConstraintProviderService>();
            var provider = new ExampleFileConstraintProviderServiceProvider.Server( exampleMock.Object, _serverMock.Object );
            _serverMock.Setup( s => s.Features ).Returns( new List<IFeatureProvider>() { provider } );
            var constraints = _constraintsProvider.ParametersConstraints;
            Assert.That( constraints, Is.Not.Empty );
            Assert.That( constraints.Count, Is.EqualTo( 1 ) );
            var constraint = constraints.Single();

            Assert.That( constraint.CommandParameterIdentifier, Is.EqualTo( "tecan.sila2.parameterdefaultsprovider/tests/ExampleFileConstraintProviderService/v1/Command/FileExampleMethod/Parameter/parameter" ) );
            var dataType = DataTypeSerializer.LoadType( constraint.Constraints );
            Assert.That( dataType.Item, Is.InstanceOf<ConstrainedType>() );
            var constrained = (ConstrainedType)dataType.Item;
            Assert.That( constrained.DataType.Item, Is.EqualTo( BasicType.String ) );
            Assert.That( constrained.Constraints.Set, Is.Not.Null );
            Assert.That( constrained.Constraints.Set.Length, Is.EqualTo( 2 ) );
            var set = constrained.Constraints.Set;
            Assert.That( set, Contains.Item("Foo.txt") );
            Assert.That( set, Contains.Item( "Bar.txt" ) );
        }

        [Test]
        public void GetParameterDefaults_NoConstraints_NoConstraint()
        {
            var exampleMock = new Mock<ISiLAService>();
            var provider = new SiLAServiceProvider.Server(exampleMock.Object, _serverMock.Object);
            _serverMock.Setup( s => s.Features ).Returns( new List<IFeatureProvider>() { provider } );
            var constraints = _constraintsProvider.ParametersConstraints;
            Assert.That(constraints, Is.Null.Or.Empty);
        }
    }
}
