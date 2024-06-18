using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Tecan.Sila2.Interop.Server.MetadataProvider;

namespace Tecan.Sila2.Interop.Server.MetadataConsumerTest
{
    [Export( typeof( IMetadataConsumerTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class MetadataConsumerTest : IMetadataConsumerTest, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Queue<char> _receivedStringMetadataChannel = new Queue<char>();
        private int _plannedToSend;

        public string ReceivedStringMetadata => StringMetadataProvider.Value;

        public string ReceivedStringMetadataAsCharacters
        {
            get
            {
                if (StringMetadataProvider.Value != null)
                {
                    lock (_receivedStringMetadataChannel)
                    {
                        foreach (char c in StringMetadataProvider.Value)
                        {
                            _receivedStringMetadataChannel.Enqueue(c);
                        }
                        _plannedToSend += StringMetadataProvider.Value.Length;
                    }
                    StringMetadataProvider.Value = null;
                }
                if (_receivedStringMetadataChannel.Count == 0)
                {
                    return null;
                }
                char returnVal;
                lock (_receivedStringMetadataChannel)
                {
                    returnVal = _receivedStringMetadataChannel.Dequeue();
                    _plannedToSend--;
                    if (_receivedStringMetadataChannel.Count > 0 && _plannedToSend > 0)
                    {
                        DelayedNotify();
                    }
                }
                return returnVal.ToString();
            }
        }

        private void DelayedNotify()
        {
            Task.Delay(10).ContinueWith(t => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReceivedStringMetadataAsCharacters))));
        }

        [return: SilaIdentifier( "ReceivedStringMetadata" )]
        public string EchoStringMetadata()
        {
            return StringMetadataProvider.Value;
        }

        [return: InlineStruct]
        public UnpackMetadataResponse UnpackMetadata()
        {
            var integers = TwoIntegersMetadataProvider.Value;
            return new UnpackMetadataResponse( StringMetadataProvider.Value, integers.FirstInteger, integers.SecondInteger );
        }
    }
}
