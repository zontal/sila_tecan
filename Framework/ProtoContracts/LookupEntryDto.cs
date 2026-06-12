using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes a data transfer object for a key-value pair
    /// </summary>
    /// <typeparam name="TKey">The domain type of the key</typeparam>
    /// <typeparam name="TKeyDto">The transfer type of the key</typeparam>
    /// <typeparam name="TValue">The domain type of the value</typeparam>
    /// <typeparam name="TValueDto">The transfer type of the value</typeparam>
    [ProtoContract]
    public class LookupEntryDto<TKey, TKeyDto, TValue, TValueDto> : ISilaTransferObject<KeyValuePair<TKey, IReadOnlyList<TValue>>> where TKeyDto : ISilaTransferObject<TKey> where TValueDto : class, ISilaTransferObject<TValue>
    {
        /// <summary>
        /// Gets the transfer object for the key
        /// </summary>
        [ProtoMember( 1, Name = "key" )]
        public TKeyDto Key
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the transfer object for the value
        /// </summary>
        [ProtoMember( 2, Name = "value" )]
        public List<TValueDto> Value
        {
            get;
            set;
        }

        /// <inheritdoc />
        public KeyValuePair<TKey, IReadOnlyList<TValue>> Extract( IBinaryStore resolver )
        {
            return new KeyValuePair<TKey, IReadOnlyList<TValue>>( Key != null ? Key.Extract( resolver ) : default, Value != null ? Value.Extract( resolver ) : default );
        }

        /// <inheritdoc />
        public string GetValidationErrors()
        {
            return Key?.GetValidationErrors() + Value.Select( v => v.GetValidationErrors() );
        }
    }
}
