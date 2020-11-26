using System.Threading.Channels;

namespace Tecan.Sila2
{
    /// <summary>
    /// Class to denote common functionality for opening channels used for observable commands
    /// </summary>
    public class Channels
    {
        /// <summary>
        /// Denotes the default channel options
        /// </summary>
        public static readonly UnboundedChannelOptions DefaultOptions = new UnboundedChannelOptions()
        {
            AllowSynchronousContinuations = true,
            SingleReader = false,
            SingleWriter = true
        };

        /// <summary>
        /// Creates a state update channel
        /// </summary>
        /// <returns>A new state update channel</returns>
        public static Channel<StateUpdate> CreateStateUpdateChannel()
        {
            return Channel.CreateUnbounded<StateUpdate>( DefaultOptions );
        }

        /// <summary>
        /// Creates a new intermediates channel
        /// </summary>
        /// <typeparam name="TIntermediate">The type of intermediate results</typeparam>
        /// <returns>A channel to which intermediate results can be written</returns>
        public static Channel<TIntermediate> CreateIntermediateChannel<TIntermediate>()
        {
            return Channel.CreateUnbounded<TIntermediate>( DefaultOptions );
        }
    }
}
