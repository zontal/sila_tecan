using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.ParameterDefaultsProvider.Tests
{
    /// <summary>
    /// Verifies the reflection-free serializers the server registers for its configuration types against the XmlSerializer
    /// </summary>
    [TestFixture]
    public class ServerConfigurationSerializerTests
    {
        private readonly FileConfigurationStore _store = new FileConfigurationStore();
        private readonly List<string> _files = new List<string>();

        [OneTimeSetUp]
        public void RegisterSerializers()
        {
            RuntimeHelpers.RunClassConstructor( typeof( SiLAServer ).TypeHandle );
        }

        [TearDown]
        public void DeleteFiles()
        {
            foreach(var file in _files)
            {
                File.Delete( file );
            }
            _files.Clear();
        }

        private string CreateName<T>( out string path )
        {
            var name = "Test" + Guid.NewGuid().ToString( "N" );
            path = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, $"{name}.{typeof( T ).Name}.xml" );
            _files.Add( path );
            return name;
        }

        private static void AssertDeepEqual( object expected, object actual, string path = "$" )
        {
            if(expected == null || actual == null)
            {
                Assert.That( actual, Is.EqualTo( expected ), path );
                return;
            }
            Assert.That( actual.GetType(), Is.EqualTo( expected.GetType() ), path + " (type)" );
            var type = expected.GetType();
            if(type.IsPrimitive || type.IsEnum || expected is string)
            {
                Assert.That( actual, Is.EqualTo( expected ), path );
                return;
            }
            if(expected is IList expectedList)
            {
                var actualList = (IList)actual;
                Assert.That( actualList.Count, Is.EqualTo( expectedList.Count ), path + " (count)" );
                for(var i = 0; i < expectedList.Count; i++)
                {
                    AssertDeepEqual( expectedList[i], actualList[i], $"{path}[{i}]" );
                }
                return;
            }
            foreach(var property in type.GetProperties( BindingFlags.Public | BindingFlags.Instance ).Where( p => p.GetIndexParameters().Length == 0 ))
            {
                AssertDeepEqual( property.GetValue( expected ), property.GetValue( actual ), path + "." + property.Name );
            }
        }

        private static MetadataConfigurationSet MetadataSet( params MetadataConfiguration[] configurations )
        {
            var set = new MetadataConfigurationSet();
            set.Configurations.AddRange( configurations );
            return set;
        }

        public static IEnumerable<TestCaseData> Configurations()
        {
            yield return new TestCaseData( new ServerConfiguration() ).SetArgDisplayNames( "Empty server configuration" );
            yield return new TestCaseData( new ServerConfiguration { Identifier = Guid.NewGuid().ToString(), Port = 50052, Name = "My <Server> & \"more\"", InterfaceFilter = "192.168.*", EnableServerConnections = true, EnableServerConnectionsSpecified = true } ).SetArgDisplayNames( "Server configuration" );
            yield return new TestCaseData( new ServerConfiguration { Port = -1, EnableServerConnections = true } ).SetArgDisplayNames( "Server configuration not specified" );
            yield return new TestCaseData( new ServerConfiguration { Name = "\u00e4\u20ac \t\r\n", EnableServerConnectionsSpecified = true } ).SetArgDisplayNames( "Server configuration special characters" );
            yield return new TestCaseData( new ClientConnectionConfig() ).SetArgDisplayNames( "Empty client connections" );
            yield return new TestCaseData( new ClientConnectionConfig { Connection = new ClientConnection[0] } ).SetArgDisplayNames( "No client connections" );
            yield return new TestCaseData( new ClientConnectionConfig { Connection = new[] { new ClientConnection { Name = "A", Host = "localhost", Port = 1 }, null, new ClientConnection() } } ).SetArgDisplayNames( "Client connections" );
            yield return new TestCaseData( new ClientConnection { Name = "A", Host = "::1", Port = 65535 } ).SetArgDisplayNames( "Client connection" );
            yield return new TestCaseData( new ClientConnection() ).SetArgDisplayNames( "Empty client connection" );
            yield return new TestCaseData( new MetadataConfigurationSet() ).SetArgDisplayNames( "Empty metadata configuration" );
            yield return new TestCaseData( MetadataSet(
                new MetadataConfiguration { Identifier = "org.silastandard/core/AuthorizationService/v1/Metadata/AccessToken", Affected = new[] { "a/b/v1", "c/d/v1/Command/X" } },
                new MetadataConfiguration { Identifier = "x", Affected = new string[0] },
                new MetadataConfiguration(),
                new MetadataConfiguration { Affected = new[] { "a", null, string.Empty } } ) ).SetArgDisplayNames( "Metadata configuration" );
        }

        private void WriteAndRead<T>( T configuration ) where T : class
        {
            var serializer = new XmlSerializer( typeof( T ) );

            var name = CreateName<T>( out var path );
            _store.Write( name, configuration );
            var written = File.ReadAllText( path );
            Assert.That( written, Does.Not.Contain( "xmlns:xsd" ), "The registered serializer was not used" );
            T expected;
            using(var stream = File.OpenRead( path ))
            {
                expected = (T)serializer.Deserialize( stream );
            }
            AssertDeepEqual( expected, _store.Read<T>( name ) );

            using(var stream = File.Create( path ))
            {
                serializer.Serialize( stream, configuration );
            }
            using(var stream = File.OpenRead( path ))
            {
                expected = (T)serializer.Deserialize( stream );
            }
            AssertDeepEqual( expected, _store.Read<T>( name ) );
        }

        [TestCaseSource( nameof( Configurations ) )]
        public void Configuration_WriteAndRead_EqualsXmlSerializer( object configuration )
        {
            typeof( ServerConfigurationSerializerTests ).GetMethod( nameof( WriteAndRead ), BindingFlags.NonPublic | BindingFlags.Instance )
                .MakeGenericMethod( configuration.GetType() )
                .Invoke( this, new[] { configuration } );
        }

        private void ReadDocument<T>( string xml ) where T : class
        {
            var name = CreateName<T>( out var path );
            File.WriteAllText( path, xml, new UTF8Encoding( false ) );
            T expected;
            try
            {
                using(var stream = File.OpenRead( path ))
                {
                    expected = (T)new XmlSerializer( typeof( T ) ).Deserialize( stream );
                }
            }
            catch(InvalidOperationException)
            {
                // the store returns null for configurations it cannot read
                expected = null;
            }
            AssertDeepEqual( expected, _store.Read<T>( name ) );
        }

        private static string RepositoryRoot
        {
            get
            {
                var directory = new DirectoryInfo( TestContext.CurrentContext.TestDirectory );
                while(directory != null && !File.Exists( Path.Combine( directory.FullName, "TecanSilaSDK.sln" ) ))
                {
                    directory = directory.Parent;
                }
                return directory?.FullName ?? throw new InvalidOperationException( "Repository root not found" );
            }
        }

        public static IEnumerable<TestCaseData> Documents()
        {
            const string serverNs = "https://gitlab.com/SiLA2/vendors/sila_tecan/-/blob/master/Server/ServerConfiguration.xsd";
            const string connectionNs = "https://gitlab.com/SiLA2/vendors/sila_tecan/ClientConnection.xsd";
            yield return new TestCaseData( typeof( ServerConfiguration ), $"<?xml version=\"1.0\"?><ServerConfiguration Identifier=\"id\" Port=\" 42 \" Name=\"n\" InterfaceFilter=\"f\" EnableServerConnections=\"1\" Foo=\"bar\" xmlns=\"{serverNs}\"><Child/></ServerConfiguration>" ).SetArgDisplayNames( "Server configuration" );
            yield return new TestCaseData( typeof( ServerConfiguration ), $"<ServerConfiguration EnableServerConnections=\"false\" xmlns=\"{serverNs}\"/>" ).SetArgDisplayNames( "Server configuration disabled connections" );
            yield return new TestCaseData( typeof( ServerConfiguration ), $"<ServerConfiguration Port=\"x\" xmlns=\"{serverNs}\"/>" ).SetArgDisplayNames( "Server configuration invalid port" );
            yield return new TestCaseData( typeof( ServerConfiguration ), "<ServerConfiguration Port=\"1\"/>" ).SetArgDisplayNames( "Server configuration without namespace" );
            yield return new TestCaseData( typeof( ClientConnectionConfig ), $"<ClientConnections xmlns=\"{connectionNs}\"><Connection Name=\"a\" Host=\"h\" Port=\"1\"/><Other/><Connection/></ClientConnections>" ).SetArgDisplayNames( "Client connections" );
            yield return new TestCaseData( typeof( ClientConnectionConfig ), $"<ClientConnectionConfig xmlns=\"{connectionNs}\"/>" ).SetArgDisplayNames( "Client connections wrong root" );
            yield return new TestCaseData( typeof( ClientConnection ), "<ClientConnection Name=\"a\" Host=\"h\" Port=\"7\"/>" ).SetArgDisplayNames( "Client connection" );
            yield return new TestCaseData( typeof( ClientConnection ), "<ClientConnection xsi:nil=\"true\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"/>" ).SetArgDisplayNames( "Client connection nil" );
            yield return new TestCaseData( typeof( MetadataConfigurationSet ), "<MetadataConfigurationSet><Configurations><MetadataConfiguration Identifier=\"a\"><Affected><string>x</string><string/></Affected><Affected><string>y</string></Affected></MetadataConfiguration></Configurations><Configurations><MetadataConfiguration/></Configurations></MetadataConfigurationSet>" ).SetArgDisplayNames( "Metadata configuration" );
            yield return new TestCaseData( typeof( MetadataConfigurationSet ), "<MetadataConfigurationSet xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><Configurations><MetadataConfiguration xsi:nil=\"true\"/><MetadataConfiguration><Affected xsi:nil=\"true\"/></MetadataConfiguration></Configurations></MetadataConfigurationSet>" ).SetArgDisplayNames( "Metadata configuration nil" );
            yield return new TestCaseData( typeof( MetadataConfigurationSet ), File.ReadAllText( Path.Combine( RepositoryRoot, "Interoperability", "InteropServer", "default.MetadataConfigurationSet.xml" ) ) ).SetArgDisplayNames( "Interop metadata configuration" );
        }

        [TestCaseSource( nameof( Documents ) )]
        public void Configuration_ReadDocument_EqualsXmlSerializer( Type type, string xml )
        {
            typeof( ServerConfigurationSerializerTests ).GetMethod( nameof( ReadDocument ), BindingFlags.NonPublic | BindingFlags.Instance )
                .MakeGenericMethod( type )
                .Invoke( this, new object[] { xml } );
        }

        public class CustomConfiguration
        {
            public string Value { get; set; }
        }

        [Test]
        public void UnregisteredConfiguration_UsesXmlSerializer()
        {
            var name = CreateName<CustomConfiguration>( out var path );
            _store.Write( name, new CustomConfiguration { Value = "x" } );
            Assert.That( File.ReadAllText( path ), Does.Contain( "xmlns:xsd" ) );
            Assert.That( _store.Read<CustomConfiguration>( name ).Value, Is.EqualTo( "x" ) );
        }

        [Test]
        public void Configuration_ReadAndWriteWithDelegates_UsesDelegates()
        {
            var name = CreateName<CustomConfiguration>( out var path );
            _store.Write( name, new CustomConfiguration { Value = "y" }, ( writer, value ) => writer.WriteElementString( "Custom", value.Value ) );
            Assert.That( _store.Exists( name, typeof( CustomConfiguration ) ), Is.True );
            var read = _store.Read( name, reader => new CustomConfiguration { Value = reader.ReadElementContentAsString( "Custom", string.Empty ) } );
            Assert.That( read.Value, Is.EqualTo( "y" ) );
            Assert.That( _store.Read<CustomConfiguration>( name, reader => throw new InvalidOperationException() ), Is.Null );
            Assert.That( _store.Read<CustomConfiguration>( "DoesNotExist" + Guid.NewGuid(), reader => throw new AssertionException( "must not be called" ) ), Is.Null );
        }
    }
}
