using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;

namespace Tecan.Sila2.Interop.Server.BinaryTransferTest
{
    [Export( typeof( IBinaryTransferTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class BinaryTransferTest : IBinaryTransferTest
    {
        public Stream BinaryValueDirectly
        {
            get
            {
                var stream = new MemoryStream();
                using(var writer = new StreamWriter( stream, Encoding.UTF8, leaveOpen: true ))
                {
                    writer.Write( "SiLA2_Test_String_Value" );
                }
                stream.Position = 0;
                return stream;
            }
        }

        public Stream BinaryValueDownload
        {
            get
            {
                var stream = new MemoryStream();
                using(var writer = new StreamWriter( stream, Encoding.UTF8, leaveOpen: true ))
                {
                    for(int i = 0; i < 100_000; i++)
                    {
                        writer.Write( "A_slightly_longer_SiLA2_Test_String_Value_used_to_demonstrate_the_binary_download" );
                    }
                }
                stream.Position = 0;
                return stream;
            }
        }

        [return: SilaIdentifier( "ReceivedValue" )]
        public Stream EchoBinaryValue( Stream binaryValue )
        {
            return binaryValue;
        }
    }
}
