using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Tecan.Sila2.DynamicClient;

namespace Tecan.Sila2.Interop.Server.ListDataTypeTest
{
    [Export( typeof( IListDataTypeTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class ListDataTypeTest : IListDataTypeTest
    {
        public ICollection<string> EmptyStringList => new List<string>();

        public ICollection<string> StringList => new List<string>
        {
            "SiLA 2",
            "is",
            "great"
        };

        public ICollection<long> IntegerList => new List<long> { 1, 2, 3 };

        public ICollection<TestStructure> StructureList => new List<TestStructure>
        {
            new TestStructure("SiLA2_Test_String_Value_1", 5124, 3.1415926, false, null, new DateTime(2022,8,5), new TimeSpan(12,34,56), new DateTime(2022,8,5,12,34,56), new DynamicObjectProperty("Any_Type_String_Value_1"))
        };

        [return: SilaIdentifier( "ReceivedValues" )]
        public ICollection<long> EchoIntegerList( ICollection<long> integerList )
        {
            return integerList;
        }

        [return: SilaIdentifier( "ReceivedValues" )]
        public ICollection<string> EchoStringList( ICollection<string> stringList )
        {
            return stringList;
        }

        [return: SilaIdentifier( "ReceivedValues" )]
        public ICollection<TestStructure> EchoStructureList( ICollection<TestStructure> structureList )
        {
            return structureList;
        }
    }
}
