using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tecan.Sila2
{
    /// <summary>
    /// Generic class for property responses
    /// </summary>
    /// <typeparam name="T">The type of the property</typeparam>
    [ProtoContract]
    public class PropertyResponse<T> : ISilaTransferObject
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

        /// <inheritdoc />
        public string GetValidationErrors()
        {
            if (Value is ISilaTransferObject transferObject)
            {
                return transferObject.GetValidationErrors();
            }
            else if (Value is IEnumerable<ISilaTransferObject> collection)
            {
                return string.Join( Environment.NewLine, collection.Select( dto => dto.GetValidationErrors() ).Where( err => !string.IsNullOrEmpty( err ) ) );
            }
            return null;
        }
    }
}
