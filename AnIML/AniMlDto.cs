using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tecan.AnIML;
using Tecan.AnIML.Serialization;

namespace Tecan.Sila2
{
#pragma warning disable PBN0013 // the AnIML document is transferred as a message of its own, not as a polymorphic sub-type of BinaryDto
    /// <summary>
    /// Denotes a data transfer object for AnIML documents
    /// </summary>
    [ProtoBuf.ProtoContract()]
    public class AniMlDto : BinaryDto, ISilaTransferObject<AniMlDocument>
    {
        /// <summary>
        /// Creates an empty data transfer object, used for deserialization
        /// </summary>
        public AniMlDto()
        {
        }

        /// <summary>
        /// Creates a new data transfer object for the given AnIML document
        /// </summary>
        /// <param name="document">The AnIML document</param>
        /// <param name="resolver">The binary resolver</param>
        public AniMlDto( AniMlDocument document, IBinaryStore resolver ) : base( DumpDocument( document ), resolver )
        {
        }

        private static FileInfo DumpDocument( AniMlDocument document )
        {
            var fileName = Path.GetTempFileName();
            using(var fs = File.Create( fileName ))
            {
                _serializer.Serialize( document, fs );
            }
            return new FileInfo( fileName );
        }

        private static readonly AniMlSerializer _serializer = new AniMlSerializer();

        /// <summary>
        /// Extracts the AnIML document represented by the data transfer object
        /// </summary>
        /// <param name="resolver">A component that manages the binary transfer</param>
        /// <returns>The inner AnIML document</returns>
        public new AniMlDocument Extract( IBinaryStore resolver )
        {
            return _serializer.Deserialize( base.Extract( resolver ) );
        }

        /// <summary>
        /// Creates a new data transfer object for the given AnIML document
        /// </summary>
        /// <param name="document">The AnIML document</param>
        /// <param name="resolver">The binary resolver</param>
        /// <returns>The created DTO</returns>
        public static AniMlDto Create( AniMlDocument document, IBinaryStore resolver )
        {
            return new AniMlDto( document, resolver );
        }
    }
}
