using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
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

        private static T XmlSerializerRoundTrip<T>( T configuration ) where T : class
        {
            // XmlWriter.Create escapes white space in attribute values on every runtime, so that the round trip shows what the
            // XmlSerializer mapping preserves rather than the attribute value normalization of XmlTextWriter on .NET Framework
            var serializer = new XmlSerializer( typeof( T ) );
            using(var stream = new MemoryStream())
            {
                using(var writer = XmlWriter.Create( stream ))
                {
                    serializer.Serialize( writer, configuration );
                }
                stream.Position = 0;
                return (T)serializer.Deserialize( stream );
            }
        }

        private static T ReadWithXmlSerializer<T>( string path ) where T : class
        {
            using(var stream = File.OpenRead( path ))
            {
                return (T)new XmlSerializer( typeof( T ) ).Deserialize( stream );
            }
        }

        private void WriteAndRead<T>( T configuration ) where T : class
        {
            // what the XmlSerializer preserves of the configuration
            var expected = XmlSerializerRoundTrip( configuration );

            var name = CreateName<T>( out var path );
            _store.Write( name, configuration );
            Assert.That( File.ReadAllText( path ), Does.Not.Contain( "xmlns:xsd" ), "The registered serializer was not used" );
            AssertDeepEqual( expected, ReadWithXmlSerializer<T>( path ), "written, read by XmlSerializer" );
            AssertDeepEqual( expected, _store.Read<T>( name ), "written, read by store" );

            using(var stream = File.Create( path ))
            {
                new XmlSerializer( typeof( T ) ).Serialize( stream, configuration );
            }
            AssertDeepEqual( ReadWithXmlSerializer<T>( path ), _store.Read<T>( name ), "written by XmlSerializer, read by store" );
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
                expected = ReadWithXmlSerializer<T>( path );
            }
            catch(InvalidOperationException)
            {
                // the store returns null for configurations it cannot read
                expected = null;
            }
            AssertDeepEqual( expected, _store.Read<T>( name ), xml );
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
            const string xsi = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";
            var typed = new Dictionary<string, (Type, string)>
            {
                ["Server configuration anonymous type"] = (typeof( ServerConfiguration ), $"<ServerConfiguration xsi:type=\"ServerConfiguration\" Port=\"1\" xmlns=\"{serverNs}\" {xsi}/>"),
                ["Server configuration other type"] = (typeof( ServerConfiguration ), $"<ServerConfiguration xsi:type=\"Other\" Port=\"1\" xmlns=\"{serverNs}\" {xsi}/>"),
                ["Client connections type"] = (typeof( ClientConnectionConfig ), $"<ClientConnections xsi:type=\"ClientConnectionConfig\" xmlns=\"{connectionNs}\" {xsi}><Connection Name=\"a\"/></ClientConnections>"),
                ["Client connections prefixed type"] = (typeof( ClientConnectionConfig ), $"<c:ClientConnections xsi:type=\"c:ClientConnectionConfig\" xmlns:c=\"{connectionNs}\" {xsi}><c:Connection Name=\"a\"/></c:ClientConnections>"),
                ["Client connections other type"] = (typeof( ClientConnectionConfig ), $"<ClientConnections xsi:type=\"ClientConnections\" xmlns=\"{connectionNs}\" {xsi}/>"),
                ["Client connections type without namespace"] = (typeof( ClientConnectionConfig ), $"<c:ClientConnections xsi:type=\"ClientConnectionConfig\" xmlns:c=\"{connectionNs}\" {xsi}/>"),
                ["Connection type"] = (typeof( ClientConnectionConfig ), $"<ClientConnections xmlns=\"{connectionNs}\" {xsi}><Connection xsi:type=\"ClientConnection\" Name=\"a\"/></ClientConnections>"),
                ["Connection other type"] = (typeof( ClientConnectionConfig ), $"<ClientConnections xmlns=\"{connectionNs}\" {xsi}><Connection Name=\"a\"/><Connection xsi:type=\"ClientConnectionConfig\" Name=\"b\"/></ClientConnections>"),
                ["Connection 2000 type"] = (typeof( ClientConnectionConfig ), $"<ClientConnections xmlns=\"{connectionNs}\" xmlns:x=\"http://www.w3.org/2000/10/XMLSchema-instance\"><Connection x:type=\"Other\" Name=\"a\"/></ClientConnections>"),
                ["Client connection type"] = (typeof( ClientConnection ), $"<ClientConnection xsi:type=\"c:ClientConnection\" xmlns:c=\"{connectionNs}\" Name=\"a\" {xsi}/>"),
                ["Client connection type without namespace"] = (typeof( ClientConnection ), $"<ClientConnection xsi:type=\"ClientConnection\" Name=\"a\" {xsi}/>"),
                ["Client connection nil other type"] = (typeof( ClientConnection ), $"<ClientConnection xsi:nil=\"true\" xsi:type=\"Other\" {xsi}/>"),
                ["Client connection undefined prefix"] = (typeof( ClientConnection ), $"<ClientConnection xsi:type=\"p:ClientConnection\" {xsi}/>"),
                ["Metadata set type"] = (typeof( MetadataConfigurationSet ), $"<MetadataConfigurationSet xsi:type=\"MetadataConfigurationSet\" {xsi}><Configurations><MetadataConfiguration Identifier=\"a\"/></Configurations></MetadataConfigurationSet>"),
                ["Metadata set other type"] = (typeof( MetadataConfigurationSet ), $"<MetadataConfigurationSet xsi:type=\"Other\" {xsi}/>"),
                ["Metadata set nil other type"] = (typeof( MetadataConfigurationSet ), $"<MetadataConfigurationSet xsi:nil=\"true\" xsi:type=\"Other\" {xsi}/>"),
                ["Metadata configuration type"] = (typeof( MetadataConfigurationSet ), $"<MetadataConfigurationSet {xsi}><Configurations><MetadataConfiguration xsi:type=\"MetadataConfiguration\" Identifier=\"a\"/></Configurations></MetadataConfigurationSet>"),
                ["Metadata configuration other type"] = (typeof( MetadataConfigurationSet ), $"<MetadataConfigurationSet {xsi}><Configurations><MetadataConfiguration Identifier=\"a\"/><MetadataConfiguration xsi:type=\"MetadataConfigurationSet\" Identifier=\"b\"/></Configurations></MetadataConfigurationSet>"),
                ["Metadata configuration nil other type"] = (typeof( MetadataConfigurationSet ), $"<MetadataConfigurationSet {xsi}><Configurations><MetadataConfiguration xsi:nil=\"true\" xsi:type=\"Other\"/></Configurations></MetadataConfigurationSet>"),
                ["Metadata wrappers typed"] = (typeof( MetadataConfigurationSet ), $"<MetadataConfigurationSet xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" {xsi}><Configurations xsi:type=\"Other\"><MetadataConfiguration Identifier=\"a\"><Affected xsi:type=\"Other\"><string xsi:type=\"xsd:int\">1</string></Affected></MetadataConfiguration></Configurations></MetadataConfigurationSet>"),
                ["Metadata strings typed"] = (typeof( MetadataConfigurationSet ), $"<MetadataConfigurationSet xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" {xsi}><Configurations><MetadataConfiguration><Affected><string xsi:type=\"xsd:string\">a</string><string xsi:type=\"Other\">b</string></Affected></MetadataConfiguration></Configurations></MetadataConfigurationSet>"),
                ["Client connection null"] = (typeof( ClientConnection ), $"<ClientConnection xsi:null=\"true\" Name=\"a\" {xsi}/>"),
                ["Client connection 1999 null"] = (typeof( ClientConnection ), "<ClientConnection x:null=\"1\" Name=\"a\" xmlns:x=\"http://www.w3.org/1999/XMLSchema-instance\"/>"),
                ["Client connection 2000 null false"] = (typeof( ClientConnection ), "<ClientConnection x:null=\"false\" Name=\"a\" xmlns:x=\"http://www.w3.org/2000/10/XMLSchema-instance\"/>"),
                ["Client connection 2000 nil"] = (typeof( ClientConnection ), "<ClientConnection x:nil=\"true\" Name=\"a\" xmlns:x=\"http://www.w3.org/2000/10/XMLSchema-instance\"/>"),
                ["Connection null"] = (typeof( ClientConnectionConfig ), $"<ClientConnections xmlns=\"{connectionNs}\" {xsi}><Connection xsi:nil=\"true\" Name=\"a\"/><Connection xsi:null=\"true\" Name=\"b\"/></ClientConnections>"),
                ["Metadata set 2000 null"] = (typeof( MetadataConfigurationSet ), "<MetadataConfigurationSet x:null=\"true\" xmlns:x=\"http://www.w3.org/2000/10/XMLSchema-instance\"><Configurations><MetadataConfiguration Identifier=\"a\"/></Configurations></MetadataConfigurationSet>"),
                ["Metadata items null"] = (typeof( MetadataConfigurationSet ), "<MetadataConfigurationSet xmlns:x=\"http://www.w3.org/1999/XMLSchema-instance\"><Configurations x:null=\"true\"><MetadataConfiguration Identifier=\"a\"/></Configurations><Configurations><MetadataConfiguration x:null=\"true\" Identifier=\"b\"/><MetadataConfiguration Identifier=\"c\"><Affected x:null=\"true\"><string>x</string></Affected><Affected><string x:null=\"true\">y</string></Affected></MetadataConfiguration></Configurations></MetadataConfigurationSet>"),
            };
            foreach(var document in typed)
            {
                yield return new TestCaseData( document.Value.Item1, document.Value.Item2 ).SetArgDisplayNames( document.Key );
            }
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
