using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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
                var encoded = Encoding.UTF8.GetBytes( "SiLA2_Test_String_Value" );
                stream.Write( encoded, 0, encoded.Length );
                stream.Position = 0;
                return stream;
            }
        }

        public Stream BinaryValueDownload
        {
            get
            {
                var stream = new MemoryStream();
                var encoded = Encoding.UTF8.GetBytes( "A_slightly_longer_SiLA2_Test_String_Value_used_to_demonstrate_the_binary_download" );
                for(int i = 0; i < 100_000; i++)
                {
                    stream.Write( encoded, 0, encoded.Length );
                }
                stream.Position = 0;
                return stream;
            }
        }

        [return: SilaIdentifier( "JointBinary" )]
        public IIntermediateObservableCommand<Stream, Stream> EchoBinariesObservably( ICollection<Stream> binaries )
        {
            return ObservableCommands.Create<Stream, Stream>( ( updates, intermediates ) => EchoBinariesObservablyCore( binaries, intermediates ) );
        }

        private async Task<Stream> EchoBinariesObservablyCore( ICollection<Stream> binaries, Action<Stream> intermediates )
        {
            var ms = new MemoryStream();
            foreach(var item in binaries)
            {
                await Task.Delay( TimeSpan.FromSeconds( 1 ) );
                await item.CopyToAsync( ms );
                item.Position = 0;
                intermediates( item );
            }
            ms.Position = 0;
            return ms;
        }

        [return: SilaIdentifier( "ReceivedValue" )]
        public Stream EchoBinaryValue( Stream binaryValue )
        {
            return binaryValue;
        }
    }
}
