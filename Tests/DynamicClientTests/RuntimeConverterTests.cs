using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tecan.Sila2.DynamicClient.Test
{
    [TestFixture]
    public class RuntimeConverterTests
    {
        [TestCase( true, BasicType.Boolean)]
        [TestCase( false, BasicType.Boolean )]
        [TestCase( 0, BasicType.Integer )]
        [TestCase( 8, BasicType.Integer )]
        [TestCase( 15L, BasicType.Integer )]
        [TestCase( 42.0, BasicType.Real )]
        [TestCase( double.NaN, BasicType.Real )]
        [TestCase( 8.15f, BasicType.Real )]
        [TestCase( "Foo", BasicType.String )]
        public void Convert_BasicObject_Successful(object source, BasicType expectedType)
        {
            var converted = RuntimeObjectConverter.Convert( source, "Test", null );
            Assert.That( converted.Type.Item, Is.EqualTo( expectedType ) );
            Assert.That( converted.Value, Is.EqualTo( source ) );
        }

        [Test]
        public void Convert_List_Successful()
        {
            var list = new List<int>() { 0, 8, 15 };
            var converted = RuntimeObjectConverter.Convert( list, "Test", null );
            Assert.That( converted.Type.Item, Is.InstanceOf<ListType>() );
            var elementType = (converted.Type.Item as ListType).DataType;
            Assert.That( elementType.Item, Is.EqualTo( BasicType.Integer ) );
            Assert.That( converted.Value, Is.EquivalentTo( list ) );
        }

        [Test]
        public void Convert_Structure_Successful()
        {
            var converted = RuntimeObjectConverter.Convert( new FooBar(), "Test", null );
            Assert.That( converted.Type.Item, Is.InstanceOf<StructureType>() );
            var structure = converted.Type.Item as StructureType;
            Assert.That( structure.Element.Length, Is.EqualTo( 2 ) );

            var fooProperty = structure.Element.Single( p => p.Identifier == nameof( FooBar.Foo ) );
            var barProperty = structure.Element.Single( p => p.Identifier == nameof( FooBar.Bar ) );

            Assert.That( fooProperty.DataType.Item, Is.EqualTo( BasicType.String ) );
            Assert.That( barProperty.DataType.Item, Is.EqualTo( BasicType.Integer ) );

            Assert.That( converted.Value, Is.InstanceOf<DynamicObject>() );
            var obj = converted.Value as DynamicObject;
            var fooValue = obj.Elements.Single( p => p.Identifier == nameof( FooBar.Foo ) );
            var barValue = obj.Elements.Single( p => p.Identifier == nameof( FooBar.Bar ) );

            Assert.That( fooValue.Value, Is.EqualTo( "Foo" ) );
            Assert.That( barValue.Value, Is.EqualTo( 42 ) );
        }

        private class FooBar
        {
            public string Foo => "Foo";

            public int Bar => 42;
        }
    }
}
