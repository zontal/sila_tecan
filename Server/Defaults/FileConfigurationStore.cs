using System;
using System.IO;
using System.Xml.Serialization;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Default implementation of a configuration store
    /// </summary>
    public class FileConfigurationStore : IConfigurationStore
    {
        /// <inheritdoc />
        public T Read<T>( string name )
        {
            var path = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, name );
            if( File.Exists( path ) )
            {
                var serializer = new XmlSerializer( typeof(T) );
                using( var stream = File.OpenRead( path ) )
                {
                    return (T)serializer.Deserialize( stream );
                }
            }
            else
            {
                return default(T);
            }
        }
    }
}
