using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Default implementation of a configuration store
    /// </summary>
    public class FileConfigurationStore : IConfigurationStore
    {
        private readonly string _basePath = AppDomain.CurrentDomain.BaseDirectory;

        /// <inheritdoc />
        public bool Exists( string name, Type type )
        {
            var path = GetFileName(name, type);
            return File.Exists( path );
        }

        private string GetFileName( string name, Type type )
        {
            return Path.Combine( _basePath, $"{name}.{type.Name}.xml");
        }

        /// <inheritdoc />
        public IEnumerable<string> GetNames( Type configurationType )
        {
            var suffix = "." + configurationType.Name + ".xml";
            var files = Directory.EnumerateFiles( _basePath, "*" + suffix );
            return files.Select( file =>
             {
                 var fileName = Path.GetFileName( file );
                 return fileName.Substring( 0, fileName.Length - suffix.Length );
             } );
        }

        /// <inheritdoc />
        public T Read<T>( string name )
        {
            var path = GetFileName( name, typeof( T ) );
            if( File.Exists( path ) )
            {
                try
                {
                    var serializer = new XmlSerializer( typeof( T ) );
                    using(var stream = File.OpenRead( path ))
                    {
                        return (T)serializer.Deserialize( stream );
                    }
                }
                catch(Exception)
                {
                    // do nothing
                }
            }
            return default;
        }

        /// <inheritdoc />
        public void Write<T>( string name, T value )
        {
            var path = GetFileName( name, typeof( T ) );

            var serializer = new XmlSerializer( typeof( T ) );
            using(var stream = File.Create( path ))
            {
                serializer.Serialize( stream, value );
            }
        }

        /// <inheritdoc />
        public bool Delete( string name, Type type )
        {
            var path = GetFileName( name, type );
            if (File.Exists( path ))
            {
                File.Delete( path );
                return true;
            }
            return false;
        }
    }
}
