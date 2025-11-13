using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using Tecan.Sila2.DynamicClient;

namespace Tecan.Sila2.Interop.Server.StructureDataTypeTest
{
    [Export( typeof( IStructureDataTypeTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class StructureDataTypeTest : IStructureDataTypeTest
    {
        public TestStructure StructureValue => new TestStructure(
            "SiLA2_Test_String_Value",
            5124,
            3.1415926,
            true,
            new MemoryStream( Encoding.ASCII.GetBytes( "SiLA2_Binary_String_Value" ) ),
            new DateTime( 2022, 8, 5 ),
            new TimeSpan( 0, 12, 34, 56, 789 ),
            new DateTime( 2022, 8, 5, 12, 34, 56 ),
            new DynamicObjectProperty( "SiLA2_Any_Type_String_Value" ) );

        public DeepStructure DeepStructureValue => new DeepStructure(
            "Outer_Test_String",
            1111,
            new DeepStructureMiddleStructure(
                "Middle_Test_String",
                2222,
                new DeepStructureMiddleStructureInnerStructure(
                    "Inner_Test_String",
                    3333 ) ) );

        [return: SilaIdentifier( "ReceivedValues" )]
        public DeepStructure EchoDeepStructureValue( [SilaDisplayName( "DeepStructure Value" )] DeepStructure deepStructureValue )
        {
            return deepStructureValue;
        }

        [return: SilaIdentifier( "ReceivedValues" )]
        public TestStructure EchoStructureValue( TestStructure structureValue )
        {
            return structureValue;
        }
    }
}
