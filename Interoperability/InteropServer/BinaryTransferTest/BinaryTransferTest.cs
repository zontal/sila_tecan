using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Interop.Server.BinaryTransferTest
{
    [Export( typeof( IBinaryTransferTest ) )]
    [Export( typeof( IRequestInterceptor ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class BinaryTransferTest : StringInterceptorBase, IBinaryTransferTest
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

        public IRequestInterceptor String => this;

        public override bool AppliesToCommands => true;

        public override bool AppliesToProperties => false;

        public override int Priority => 0;

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

        private string _stringCollectedByMetadata;

        [return: InlineStruct]
        public EchoBinaryAndMetadataStringResponse EchoBinaryAndMetadataString(Stream binary)
        {
            return new EchoBinaryAndMetadataStringResponse(binary, _stringCollectedByMetadata);
        }

        public override bool IsInterceptRequired(Feature feature, string commandIdentifier)
        {
            return commandIdentifier == "org.silastandard/test/BinaryTransferTest/v1/Command/EchoBinaryAndMetadataString";
        }

        public override IRequestInterception Intercept(string commandIdentifier, string @string)
        {
            _stringCollectedByMetadata = @string;
            return null;
        }
    }
}
