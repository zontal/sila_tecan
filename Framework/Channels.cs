using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace Tecan.Sila2
{
    internal class Channels
    {
        public static readonly UnboundedChannelOptions DefaultOptions = new UnboundedChannelOptions()
        {
            AllowSynchronousContinuations = true,
            SingleReader = false,
            SingleWriter = true
        };

        public static Channel<StateUpdate> CreateStateUpdateChannel()
        {
            return Channel.CreateUnbounded<StateUpdate>( DefaultOptions );
        }

        public static Channel<TIntermediate> CreateIntermediateChannel<TIntermediate>()
        {
            return Channel.CreateUnbounded<TIntermediate>( DefaultOptions );
        }
    }
}
