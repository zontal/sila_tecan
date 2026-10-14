using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if NET
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endif
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Tecan.Sila2
{
    /// <summary>
    /// Default implementation of a configuration store
    /// </summary>
    /// <remarks>
    /// Configurations are stored as XML files. For configuration types with a serializer registered through
    /// <see cref="RegisterSerializer{T}(Func{XmlReader, T}, Action{XmlWriter, T})"/>, the store uses that serializer,
    /// otherwise it uses the reflection-based <see cref="XmlSerializer"/>, which is not available in Native AOT applications.
    /// </remarks>
    public class FileConfigurationStore : IConfigurationStore
    {
        private const string XmlSerializerUnreferencedCodeMessage = "The XmlSerializer requires the members of the configuration type, which may be removed by trimming. Register a serializer for the configuration type with FileConfigurationStore.RegisterSerializer.";
        private const string XmlSerializerDynamicCodeMessage = "The XmlSerializer may generate code at runtime. Register a serializer for the configuration type with FileConfigurationStore.RegisterSerializer.";

        private static readonly ConcurrentDictionary<Type, object> Serializers = new ConcurrentDictionary<Type, object>();

        private static readonly XmlReaderSettings ReaderSettings = new XmlReaderSettings
        {
            IgnoreWhitespace = true
        };

        private static readonly XmlWriterSettings WriterSettings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding( false ),
            Indent = true
        };

        private readonly string _basePath = AppDomain.CurrentDomain.BaseDirectory;

        private sealed class ConfigurationSerializer<T>
        {
            public ConfigurationSerializer( Func<XmlReader, T> deserialize, Action<XmlWriter, T> serialize )
            {
                Deserialize = deserialize;
                Serialize = serialize;
            }

            public Func<XmlReader, T> Deserialize { get; }

            public Action<XmlWriter, T> Serialize { get; }
        }

        /// <summary>
        /// Registers a serializer for configurations of the given type that the configuration stores use instead of the reflection-based XmlSerializer
        /// </summary>
        /// <typeparam name="T">The type of the configuration</typeparam>
        /// <param name="deserialize">A function that reads the configuration from a reader positioned on the root element of the document</param>
        /// <param name="serialize">A function that writes the root element of the configuration document</param>
        /// <remarks>Registering a serializer makes <see cref="Read{T}(string)"/> and <see cref="Write{T}(string, T)"/> safe for trimming and Native AOT for this configuration type.</remarks>
        public static void RegisterSerializer<T>( Func<XmlReader, T> deserialize, Action<XmlWriter, T> serialize ) where T : class
        {
            if(deserialize == null)
            {
                throw new ArgumentNullException( nameof( deserialize ) );
            }
            if(serialize == null)
            {
                throw new ArgumentNullException( nameof( serialize ) );
            }
            Serializers[typeof( T )] = new ConfigurationSerializer<T>( deserialize, serialize );
        }

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
        /// <remarks>
        /// If no serializer is registered for the configuration type, the configuration is read with the XmlSerializer.
        /// In applications without support for dynamic code (Native AOT), this throws a <see cref="NotSupportedException"/> instead.
        /// </remarks>
        public T Read<T>( string name ) where T : class
        {
            if(Serializers.TryGetValue( typeof( T ), out var serializer ))
            {
                return Read( name, ( (ConfigurationSerializer<T>)serializer ).Deserialize );
            }
            var path = GetFileName( name, typeof( T ) );
            if( File.Exists( path ) )
            {
#if NET
                if(!RuntimeFeature.IsDynamicCodeSupported)
                {
                    throw MissingSerializer( typeof( T ) );
                }
#endif
                return ReadWithXmlSerializer<T>( path );
            }
            return default;
        }

        /// <summary>
        /// Reads a configuration of the given type with the given deserializer
        /// </summary>
        /// <typeparam name="T">The type of the configuration</typeparam>
        /// <param name="name">The name of the configuration file</param>
        /// <param name="deserialize">A function that reads the configuration from a reader positioned on the root element of the document</param>
        /// <returns>The deserialized configuration or null, if the configuration does not exist or cannot be read</returns>
        public T Read<T>( string name, Func<XmlReader, T> deserialize ) where T : class
        {
            if(deserialize == null)
            {
                throw new ArgumentNullException( nameof( deserialize ) );
            }
            var path = GetFileName( name, typeof( T ) );
            if( File.Exists( path ) )
            {
                try
                {
                    using(var stream = File.OpenRead( path ))
                    using(var reader = XmlReader.Create( stream, ReaderSettings ))
                    {
                        reader.MoveToContent();
                        return deserialize( reader );
                    }
                }
                catch(Exception)
                {
                    // do nothing
                }
            }
            return default;
        }

#if NET
        [RequiresUnreferencedCode( XmlSerializerUnreferencedCodeMessage )]
        [RequiresDynamicCode( XmlSerializerDynamicCodeMessage )]
#endif
        private static T ReadWithXmlSerializer<T>( string path ) where T : class
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
            return default;
        }

        /// <inheritdoc />
        /// <remarks>
        /// If no serializer is registered for the configuration type, the configuration is written with the XmlSerializer.
        /// In applications without support for dynamic code (Native AOT), this throws a <see cref="NotSupportedException"/> instead.
        /// </remarks>
        public void Write<T>( string name, T value ) where T : class
        {
            if(Serializers.TryGetValue( typeof( T ), out var serializer ))
            {
                Write( name, value, ( (ConfigurationSerializer<T>)serializer ).Serialize );
                return;
            }
#if NET
            if(!RuntimeFeature.IsDynamicCodeSupported)
            {
                throw MissingSerializer( typeof( T ) );
            }
#endif
            WriteWithXmlSerializer( GetFileName( name, typeof( T ) ), value );
        }

        /// <summary>
        /// Writes the configuration with the given name using the given serializer
        /// </summary>
        /// <typeparam name="T">The data type of the configuration</typeparam>
        /// <param name="name">The name of the configuration file</param>
        /// <param name="value">The configuration to store</param>
        /// <param name="serialize">A function that writes the root element of the configuration document</param>
        public void Write<T>( string name, T value, Action<XmlWriter, T> serialize ) where T : class
        {
            if(serialize == null)
            {
                throw new ArgumentNullException( nameof( serialize ) );
            }
            var path = GetFileName( name, typeof( T ) );
            using(var stream = File.Create( path ))
            using(var writer = XmlWriter.Create( stream, WriterSettings ))
            {
                writer.WriteStartDocument();
                serialize( writer, value );
                writer.WriteEndDocument();
            }
        }

#if NET
        [RequiresUnreferencedCode( XmlSerializerUnreferencedCodeMessage )]
        [RequiresDynamicCode( XmlSerializerDynamicCodeMessage )]
#endif
        private static void WriteWithXmlSerializer<T>( string path, T value ) where T : class
        {
            var serializer = new XmlSerializer( typeof( T ) );
            using(var stream = File.Create( path ))
            {
                serializer.Serialize( stream, value );
            }
        }

        private static NotSupportedException MissingSerializer( Type type )
        {
            return new NotSupportedException( $"No serializer is registered for the configuration type {type.FullName} and the XmlSerializer is not supported without dynamic code. Register a serializer with {nameof( FileConfigurationStore )}.{nameof( RegisterSerializer )}." );
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
