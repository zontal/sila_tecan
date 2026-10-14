using System;
using System.Collections.Generic;
using System.Xml;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Reflection-free serializers for the configuration types of the server. They read and write the same XML documents
    /// as the XmlSerializer and are registered with the <see cref="FileConfigurationStore"/>, so that the server does not
    /// depend on the XmlSerializer and works when trimmed or compiled with Native AOT.
    /// </summary>
    internal static class ConfigurationSerializers
    {
        private const string ServerConfigurationNamespace = "https://gitlab.com/SiLA2/vendors/sila_tecan/-/blob/master/Server/ServerConfiguration.xsd";
        private const string ClientConnectionNamespace = "https://gitlab.com/SiLA2/vendors/sila_tecan/ClientConnection.xsd";
        private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

        private static readonly bool Registered = Register();

        /// <summary>
        /// Makes sure that the serializers are registered with the file configuration store
        /// </summary>
        public static void EnsureRegistered()
        {
            if(!Registered)
            {
                throw new InvalidOperationException( "The configuration serializers could not be registered." );
            }
        }

        private static bool Register()
        {
            FileConfigurationStore.RegisterSerializer( ReadServerConfiguration, WriteServerConfiguration );
            FileConfigurationStore.RegisterSerializer( ReadClientConnectionConfig, WriteClientConnectionConfig );
            FileConfigurationStore.RegisterSerializer( ReadClientConnection, WriteClientConnection );
            FileConfigurationStore.RegisterSerializer( ReadMetadataConfigurationSet, WriteMetadataConfigurationSet );
            return true;
        }

        #region Reading

        private static void ReadRoot( XmlReader reader, string localName, string ns )
        {
            reader.MoveToContent();
            if(reader.NodeType != XmlNodeType.Element || reader.LocalName != localName || reader.NamespaceURI != ns)
            {
                throw new InvalidOperationException( $"<{reader.LocalName} xmlns='{reader.NamespaceURI}'> was not expected." );
            }
        }

        private static bool IsNil( XmlReader reader )
        {
            var nil = reader.GetAttribute( "nil", XsiNamespace );
            return nil != null && XmlConvert.ToBoolean( nil );
        }

        /// <summary>
        /// Reads the child elements of the current element, skipping elements the callback does not accept
        /// </summary>
        private static void ReadChildren( XmlReader reader, Func<XmlReader, bool> readElement )
        {
            reader.MoveToElement();
            if(reader.IsEmptyElement)
            {
                reader.Skip();
                return;
            }
            reader.ReadStartElement();
            reader.MoveToContent();
            while(reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.None)
            {
                if(reader.NodeType != XmlNodeType.Element || !readElement( reader ))
                {
                    reader.Skip();
                }
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        private static ServerConfiguration ReadServerConfiguration( XmlReader reader )
        {
            ReadRoot( reader, nameof( ServerConfiguration ), ServerConfigurationNamespace );
            var configuration = new ServerConfiguration();
            bool identifierRead = false, portRead = false, nameRead = false, filterRead = false, connectionsRead = false;
            while(reader.MoveToNextAttribute())
            {
                if(reader.NamespaceURI.Length != 0)
                {
                    continue;
                }
                switch(reader.LocalName)
                {
                    case nameof( ServerConfiguration.Identifier ) when !identifierRead:
                        configuration.Identifier = reader.Value;
                        identifierRead = true;
                        break;
                    case nameof( ServerConfiguration.Port ) when !portRead:
                        configuration.Port = XmlConvert.ToInt32( reader.Value );
                        portRead = true;
                        break;
                    case nameof( ServerConfiguration.Name ) when !nameRead:
                        configuration.Name = reader.Value;
                        nameRead = true;
                        break;
                    case nameof( ServerConfiguration.InterfaceFilter ) when !filterRead:
                        configuration.InterfaceFilter = reader.Value;
                        filterRead = true;
                        break;
                    case nameof( ServerConfiguration.EnableServerConnections ) when !connectionsRead:
                        configuration.EnableServerConnectionsSpecified = true;
                        configuration.EnableServerConnections = XmlConvert.ToBoolean( reader.Value );
                        connectionsRead = true;
                        break;
                }
            }
            ReadChildren( reader, r => false );
            return configuration;
        }

        private static ClientConnectionConfig ReadClientConnectionConfig( XmlReader reader )
        {
            ReadRoot( reader, "ClientConnections", ClientConnectionNamespace );
            var configuration = new ClientConnectionConfig();
            List<ClientConnection> connections = null;
            ReadChildren( reader, r =>
            {
                if(r.LocalName == "Connection" && r.NamespaceURI == ClientConnectionNamespace)
                {
                    connections = connections ?? new List<ClientConnection>();
                    connections.Add( ReadClientConnectionContent( r ) );
                    return true;
                }
                return false;
            } );
            configuration.Connection = connections?.ToArray();
            return configuration;
        }

        private static ClientConnection ReadClientConnection( XmlReader reader )
        {
            // the type has no root attribute, so the XmlSerializer uses the type name without namespace for the root element
            ReadRoot( reader, nameof( ClientConnection ), string.Empty );
            if(IsNil( reader ))
            {
                reader.Skip();
                return null;
            }
            return ReadClientConnectionContent( reader );
        }

        private static ClientConnection ReadClientConnectionContent( XmlReader reader )
        {
            var connection = new ClientConnection();
            bool nameRead = false, hostRead = false, portRead = false;
            while(reader.MoveToNextAttribute())
            {
                if(reader.NamespaceURI.Length != 0)
                {
                    continue;
                }
                switch(reader.LocalName)
                {
                    case nameof( ClientConnection.Name ) when !nameRead:
                        connection.Name = reader.Value;
                        nameRead = true;
                        break;
                    case nameof( ClientConnection.Host ) when !hostRead:
                        connection.Host = reader.Value;
                        hostRead = true;
                        break;
                    case nameof( ClientConnection.Port ) when !portRead:
                        connection.Port = XmlConvert.ToInt32( reader.Value );
                        portRead = true;
                        break;
                }
            }
            ReadChildren( reader, r => false );
            return connection;
        }

        private static MetadataConfigurationSet ReadMetadataConfigurationSet( XmlReader reader )
        {
            ReadRoot( reader, nameof( MetadataConfigurationSet ), string.Empty );
            if(IsNil( reader ))
            {
                reader.Skip();
                return null;
            }
            var configurationSet = new MetadataConfigurationSet();
            ReadChildren( reader, r =>
            {
                if(r.LocalName != nameof( MetadataConfigurationSet.Configurations ) || r.NamespaceURI.Length != 0)
                {
                    return false;
                }
                if(IsNil( r ))
                {
                    r.Skip();
                    return true;
                }
                // the collection is read-only, so the XmlSerializer adds the items to the existing collection
                ReadChildren( r, item =>
                {
                    if(item.LocalName != nameof( MetadataConfiguration ) || item.NamespaceURI.Length != 0)
                    {
                        return false;
                    }
                    if(IsNil( item ))
                    {
                        item.Skip();
                        configurationSet.Configurations.Add( null );
                        return true;
                    }
                    configurationSet.Configurations.Add( ReadMetadataConfiguration( item ) );
                    return true;
                } );
                return true;
            } );
            return configurationSet;
        }

        private static MetadataConfiguration ReadMetadataConfiguration( XmlReader reader )
        {
            var configuration = new MetadataConfiguration();
            var identifierRead = false;
            while(reader.MoveToNextAttribute())
            {
                if(!identifierRead && reader.LocalName == nameof( MetadataConfiguration.Identifier ) && reader.NamespaceURI.Length == 0)
                {
                    configuration.Identifier = reader.Value;
                    identifierRead = true;
                }
            }
            ReadChildren( reader, r =>
            {
                if(r.LocalName != nameof( MetadataConfiguration.Affected ) || r.NamespaceURI.Length != 0)
                {
                    return false;
                }
                if(IsNil( r ))
                {
                    r.Skip();
                    return true;
                }
                var affected = new List<string>();
                ReadChildren( r, item =>
                {
                    if(item.LocalName != "string" || item.NamespaceURI.Length != 0)
                    {
                        return false;
                    }
                    if(IsNil( item ))
                    {
                        item.Skip();
                        affected.Add( null );
                    }
                    else
                    {
                        affected.Add( item.ReadElementContentAsString() );
                    }
                    return true;
                } );
                configuration.Affected = affected.ToArray();
                return true;
            } );
            return configuration;
        }

        #endregion

        #region Writing

        private static void WriteAttribute( XmlWriter writer, string localName, string value )
        {
            if(value != null)
            {
                writer.WriteAttributeString( localName, string.Empty, value );
            }
        }

        private static void WriteServerConfiguration( XmlWriter writer, ServerConfiguration configuration )
        {
            writer.WriteStartElement( nameof( ServerConfiguration ), ServerConfigurationNamespace );
            if(configuration != null)
            {
                WriteAttribute( writer, nameof( ServerConfiguration.Identifier ), configuration.Identifier );
                WriteAttribute( writer, nameof( ServerConfiguration.Port ), XmlConvert.ToString( configuration.Port ) );
                WriteAttribute( writer, nameof( ServerConfiguration.Name ), configuration.Name );
                WriteAttribute( writer, nameof( ServerConfiguration.InterfaceFilter ), configuration.InterfaceFilter );
                if(configuration.EnableServerConnectionsSpecified)
                {
                    WriteAttribute( writer, nameof( ServerConfiguration.EnableServerConnections ), XmlConvert.ToString( configuration.EnableServerConnections ) );
                }
            }
            writer.WriteEndElement();
        }

        private static void WriteClientConnectionConfig( XmlWriter writer, ClientConnectionConfig configuration )
        {
            writer.WriteStartElement( "ClientConnections", ClientConnectionNamespace );
            if(configuration?.Connection != null)
            {
                foreach(var connection in configuration.Connection)
                {
                    if(connection != null)
                    {
                        writer.WriteStartElement( "Connection", ClientConnectionNamespace );
                        WriteClientConnectionContent( writer, connection );
                        writer.WriteEndElement();
                    }
                }
            }
            writer.WriteEndElement();
        }

        private static void WriteClientConnection( XmlWriter writer, ClientConnection connection )
        {
            writer.WriteStartElement( nameof( ClientConnection ), string.Empty );
            if(connection == null)
            {
                writer.WriteAttributeString( "xsi", "nil", XsiNamespace, "true" );
            }
            else
            {
                WriteClientConnectionContent( writer, connection );
            }
            writer.WriteEndElement();
        }

        private static void WriteClientConnectionContent( XmlWriter writer, ClientConnection connection )
        {
            WriteAttribute( writer, nameof( ClientConnection.Name ), connection.Name );
            WriteAttribute( writer, nameof( ClientConnection.Host ), connection.Host );
            WriteAttribute( writer, nameof( ClientConnection.Port ), XmlConvert.ToString( connection.Port ) );
        }

        private static void WriteMetadataConfigurationSet( XmlWriter writer, MetadataConfigurationSet configurationSet )
        {
            writer.WriteStartElement( nameof( MetadataConfigurationSet ), string.Empty );
            if(configurationSet == null)
            {
                writer.WriteAttributeString( "xsi", "nil", XsiNamespace, "true" );
                writer.WriteEndElement();
                return;
            }
            writer.WriteStartElement( nameof( MetadataConfigurationSet.Configurations ), string.Empty );
            foreach(var configuration in configurationSet.Configurations)
            {
                writer.WriteStartElement( nameof( MetadataConfiguration ), string.Empty );
                if(configuration == null)
                {
                    writer.WriteAttributeString( "xsi", "nil", XsiNamespace, "true" );
                }
                else
                {
                    WriteAttribute( writer, nameof( MetadataConfiguration.Identifier ), configuration.Identifier );
                    if(configuration.Affected != null)
                    {
                        writer.WriteStartElement( nameof( MetadataConfiguration.Affected ), string.Empty );
                        foreach(var affected in configuration.Affected)
                        {
                            writer.WriteStartElement( "string", string.Empty );
                            if(affected == null)
                            {
                                writer.WriteAttributeString( "xsi", "nil", XsiNamespace, "true" );
                            }
                            else
                            {
                                writer.WriteString( affected );
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        #endregion
    }
}
