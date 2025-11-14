using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.DynamicClient.Test
{
    [TestFixture]
    public class DataTypeSerializerTests
    {
        private const string EmptyType = "<DataType xmlns=\"http://www.sila-standard.org\"><Constrained><DataType><Basic>String</Basic></DataType><Constraints><Length>0</Length></Constraints></Constrained></DataType>";

        [Test]
        public void DataTypeSerializer_CanParseEmptyDataType()
        {
            var xml = EmptyType;

            var dataType = DataTypeSerializer.LoadType(xml);

            Assert.That(dataType, Is.Not.Null);
            Assert.That(dataType.Item, Is.InstanceOf(typeof(ConstrainedType)));
            var constrained = dataType.Item as ConstrainedType;
            Assert.That(constrained.DataType?.Item, Is.EqualTo(BasicType.String));
            Assert.That(constrained.Constraints.Length, Is.EqualTo("0"));
        }

        [Test]
        public void DataTypeSerializer_SavesEmptyDataType()
        {
            var dataType = new DataTypeType
            {
                Item = new ConstrainedType
                {
                    DataType = new DataTypeType { Item = BasicType.String },
                    Constraints = new Constraints
                    {
                        Length = "0"
                    }
                }
            };

            var xml = DataTypeSerializer.SaveToString(dataType);

            Assert.That(xml, Is.Not.Null);
        }
    }
}
