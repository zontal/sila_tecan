using System;
using ProtoBuf;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Data transfer object for the AnyType
    /// </summary>
    [ProtoContract]
    public class AnyTypeDto : ISilaTransferObject<DynamicObjectProperty>
    {
        internal static readonly DynamicObjectSerializer Serializer = new DynamicObjectSerializer(type => throw new NotSupportedException("Type references are not supported."));
        private const string Identifier = "RuntimeData";
        private const string DisplayName = "Runtime Data";
        private const string Description = "Represents data dynamically obtained at runtime";

        /// <summary>
        /// Creates a new dto
        /// </summary>
        public AnyTypeDto() { }

        /// <summary>
        /// Creates a new dto with the given content
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public AnyTypeDto( DynamicObjectProperty value, IBinaryStore store = null )
        {
            if( value != null )
            {
                Type = DataTypeSerializer.SaveToString( value.Type );
                Payload = Serializer.Serialize( value, store );
            }
        }

        /// <summary>
        /// The type of the content
        /// </summary>
        [ProtoMember( 1, Name = @"type" )]
        [System.ComponentModel.DefaultValue( "" )]
        public string Type { get; set; } = "";

        /// <summary>
        /// The payload information
        /// </summary>
        [ProtoMember( 2, Name = @"payload" )]
        public byte[] Payload { get; set; } = null;

        /// <summary>
        /// Extracts the underlying content
        /// </summary>
        /// <returns>The actual content value</returns>
        public DynamicObjectProperty Extract( IBinaryStore resolver )
        {
            if( Type == null )
            {
                return null;
            }

            var type = DataTypeSerializer.LoadType( Type );
            var property = new DynamicObjectProperty( Identifier, DisplayName, Description, type );
            Serializer.Deserialize( property, Payload, false, resolver );
            return property;
        }

        /// <summary>
        /// Gets the validation errors for this dto
        /// </summary>
        /// <returns>null, because the type does not perform validation</returns>
        public string GetValidationErrors()
        {
            return null;
        }

        /// <summary>
        /// Creates a dto for the given content
        /// </summary>
        /// <param name="value">The content value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static AnyTypeDto Create( DynamicObjectProperty value, IBinaryStore store = null )
        {
            return new AnyTypeDto(value, store);
        }
    }
}
