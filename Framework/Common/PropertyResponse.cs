using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// Generic class for property responses
    /// </summary>
    /// <typeparam name="T">The type of the property</typeparam>
    [ProtoContract]
    public class PropertyResponse<T>
    {
        /// <summary>
        /// Create a new instance
        /// </summary>
        public PropertyResponse() { }


        /// <summary>
        /// Create a new instance
        /// </summary>
        public PropertyResponse(T value)
        {
            Value = value;
        }

        /// <summary>
        /// The actual property value
        /// </summary>
        [ProtoMember(1)]
        public T Value { get; set; }
    }
}
