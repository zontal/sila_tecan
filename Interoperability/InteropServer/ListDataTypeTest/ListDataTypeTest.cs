using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
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
            new TestStructure("SiLA2_Test_String_Value_1", 5124, 3.1415926, true, CreateStream("Binary_String_Value_1"), new DateTime(2022,8,5), new TimeWithTimezone( new TimeSpan(0,12,34,56,789), TimeSpan.FromHours(2)), new DateTimeOffset(2022,8,5,12,34,56,789, TimeSpan.FromHours(2)), new DynamicObjectProperty("Any_Type_String_Value_1")),
            new TestStructure("SiLA2_Test_String_Value_2", 5125, 4.1415926, false, CreateStream("Binary_String_Value_2"), new DateTime(2023,9,6), new TimeWithTimezone( new TimeSpan(0,13,35,57,790), TimeSpan.FromHours(2)), new DateTimeOffset(2023,9,6,13,35,57,790, TimeSpan.FromHours(2)), new DynamicObjectProperty("Any_Type_String_Value_2")),
            new TestStructure("SiLA2_Test_String_Value_3", 5126, 5.1415926, true, CreateStream("Binary_String_Value_3"), new DateTime(2024,10,7), new TimeWithTimezone( new TimeSpan(0,14,36,58,791), TimeSpan.FromHours(2)), new DateTimeOffset(2024,10,7,14,36,58,791, TimeSpan.FromHours(2)), new DynamicObjectProperty("Any_Type_String_Value_3"))
        };

        private static Stream CreateStream(string embeddedValue)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(embeddedValue));
        }

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
