using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Tecan.Sila2
{
    internal static partial class FeatureDefinitionXml
    {
        private static T Deserialize<T>( TextReader textReader, string rootName, string rootNamespace, string rootTypeName, bool rootIsNullable, Func<XmlReader, T> readRoot ) where T : class
        {
            // same reader configuration as XmlSerializer.Deserialize(TextReader)
            var reader = new XmlTextReader( textReader )
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Normalization = true,
                XmlResolver = null
            };
            try
            {
                reader.MoveToContent();
                if(reader.NodeType != XmlNodeType.Element)
                {
                    SkipUnknownNode( reader );
                    return null;
                }
                if(reader.LocalName != rootName || reader.NamespaceURI != rootNamespace)
                {
                    throw new InvalidOperationException( $"<{reader.LocalName} xmlns='{reader.NamespaceURI}'> was not expected." );
                }
                if(rootIsNullable && IsNil( reader ))
                {
                    CheckType( reader, rootTypeName );
                    ReadNull( reader );
                    return null;
                }
                return readRoot( reader );
            }
            catch(Exception exception) when(!(exception is OutOfMemoryException))
            {
                throw new InvalidOperationException( string.Format( CultureInfo.InvariantCulture, "There is an error in XML document ({0}, {1}).", reader.LineNumber, reader.LinePosition ), exception );
            }
        }

        private static bool IsSilaElement( XmlReader reader, string localName )
        {
            return reader.LocalName == localName && reader.NamespaceURI == SilaNamespace;
        }

        private static void SkipUnknownNode( XmlReader reader )
        {
            if(reader.NodeType != XmlNodeType.EndElement)
            {
                reader.Skip();
            }
        }

        private static bool IsNil( XmlReader reader )
        {
            var isNull = reader.GetAttribute( "nil", XsiNamespace )
                ?? reader.GetAttribute( "null", XsiNamespace )
                ?? reader.GetAttribute( "null", Xsi2000Namespace )
                ?? reader.GetAttribute( "null", Xsi1999Namespace );
            return isNull != null && XmlConvert.ToBoolean( isNull );
        }

        private static bool ReadNull( XmlReader reader )
        {
            if(!IsNil( reader ))
            {
                return false;
            }
            if(reader.IsEmptyElement)
            {
                reader.Skip();
                return true;
            }
            reader.ReadStartElement();
            while(reader.NodeType != XmlNodeType.EndElement)
            {
                SkipUnknownNode( reader );
            }
            reader.ReadEndElement();
            return true;
        }

        /// <summary>
        /// Rejects an xsi:type that does not denote the expected type, as the XmlSerializer does
        /// </summary>
        private static void CheckType( XmlReader reader, string typeName )
        {
            var type = reader.GetAttribute( "type", XsiNamespace )
                ?? reader.GetAttribute( "type", Xsi2000Namespace )
                ?? reader.GetAttribute( "type", Xsi1999Namespace );
            if(type == null)
            {
                return;
            }
            var colon = type.LastIndexOf( ':' );
            string localName;
            string ns;
            if(colon <= 0)
            {
                localName = type;
                ns = reader.LookupNamespace( string.Empty ) ?? string.Empty;
            }
            else
            {
                var prefix = type.Substring( 0, colon );
                localName = type.Substring( colon + 1 );
                ns = reader.LookupNamespace( prefix ) ?? throw new InvalidOperationException( $"Namespace prefix '{prefix}' is not defined." );
            }
            if(localName != typeName || ns != SilaNamespace)
            {
                throw new InvalidOperationException( $"The specified type was not recognized: name='{localName}', namespace='{ns}', at <{reader.LocalName} xmlns='{reader.NamespaceURI}'>." );
            }
        }

        /// <summary>
        /// Reads the content of a complex element. The callback is invoked for every child element and returns false for unknown elements, which are skipped.
        /// </summary>
        private static void ReadContent( XmlReader reader, Func<XmlReader, bool> readElement )
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
                    SkipUnknownNode( reader );
                }
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        private static string ReadString( XmlReader reader )
        {
            // The XmlSerializer of the .NET Framework reads text elements with ReadElementString, which rejects
            // comments and entity references inside the text, the one of .NET (Core) uses ReadElementContentAsString.
            return IsNetFramework ? reader.ReadElementString() : reader.ReadElementContentAsString();
        }

        /// <summary>
        /// Reads a string mapped to an XSD integer type, which the XmlSerializer reads with collapsed whitespace
        /// </summary>
        private static string ReadIntegerString( XmlReader reader )
        {
            return ReadString( reader ).Trim();
        }

        /// <summary>
        /// Reads a wrapped array. Like the XmlSerializer, a repeated wrapper element replaces the array read before, and a nil wrapper (null result) leaves it unchanged.
        /// </summary>
        private static string[] ReadStringArray( XmlReader reader, string itemName )
        {
            if(ReadNull( reader ))
            {
                return null;
            }
            var items = new List<string>();
            ReadContent( reader, r =>
            {
                if(IsSilaElement( r, itemName ))
                {
                    items.Add( ReadString( r ) );
                    return true;
                }
                return false;
            } );
            return items.ToArray();
        }

        private static T[] ToArray<T>( List<T> list )
        {
            return list?.ToArray();
        }

        private static void Add<T>( ref List<T> list, T item )
        {
            if(list == null)
            {
                list = new List<T>();
            }
            list.Add( item );
        }

        private static Feature ReadFeature( XmlReader reader )
        {
            CheckType( reader, null );
            var feature = new Feature();
            bool localeRead = false, silaVersionRead = false, featureVersionRead = false, maturityRead = false, originatorRead = false, categoryRead = false;
            while(reader.MoveToNextAttribute())
            {
                if(reader.NamespaceURI.Length != 0)
                {
                    continue;
                }
                switch(reader.LocalName)
                {
                    case "Locale" when !localeRead:
                        feature.Locale = reader.Value;
                        localeRead = true;
                        break;
                    case "SiLA2Version" when !silaVersionRead:
                        feature.SiLA2Version = reader.Value;
                        silaVersionRead = true;
                        break;
                    case "FeatureVersion" when !featureVersionRead:
                        feature.FeatureVersion = reader.Value;
                        featureVersionRead = true;
                        break;
                    case "MaturityLevel" when !maturityRead:
                        feature.MaturityLevel = ParseFeatureMaturityLevel( reader.Value );
                        maturityRead = true;
                        break;
                    case "Originator" when !originatorRead:
                        feature.Originator = reader.Value;
                        originatorRead = true;
                        break;
                    case "Category" when !categoryRead:
                        feature.Category = reader.Value;
                        categoryRead = true;
                        break;
                }
            }
            bool identifierRead = false, displayNameRead = false, descriptionRead = false;
            List<object> items = null;
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "Identifier" when !identifierRead:
                        feature.Identifier = ReadString( r );
                        identifierRead = true;
                        return true;
                    case "DisplayName" when !displayNameRead:
                        feature.DisplayName = ReadString( r );
                        displayNameRead = true;
                        return true;
                    case "Description" when !descriptionRead:
                        feature.Description = ReadString( r );
                        descriptionRead = true;
                        return true;
                    case "Command":
                        Add( ref items, ReadFeatureCommand( r ) );
                        return true;
                    case "DataTypeDefinition":
                        Add( ref items, ReadSiLAElement( r ) );
                        return true;
                    case "DefinedExecutionError":
                        Add( ref items, ReadFeatureDefinedExecutionError( r ) );
                        return true;
                    case "Metadata":
                        Add( ref items, ReadFeatureMetadata( r ) );
                        return true;
                    case "Property":
                        Add( ref items, ReadFeatureProperty( r ) );
                        return true;
                    default:
                        return false;
                }
            } );
            feature.Items = ToArray( items );
            return feature;
        }

        private static FeatureCommand ReadFeatureCommand( XmlReader reader )
        {
            CheckType( reader, null );
            var command = new FeatureCommand();
            bool identifierRead = false, displayNameRead = false, descriptionRead = false, observableRead = false;
            List<SiLAElement> parameters = null, responses = null, intermediateResponses = null;
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "Identifier" when !identifierRead:
                        command.Identifier = ReadString( r );
                        identifierRead = true;
                        return true;
                    case "DisplayName" when !displayNameRead:
                        command.DisplayName = ReadString( r );
                        displayNameRead = true;
                        return true;
                    case "Description" when !descriptionRead:
                        command.Description = ReadString( r );
                        descriptionRead = true;
                        return true;
                    case "Observable" when !observableRead:
                        command.Observable = ParseFeatureCommandObservable( ReadString( r ) );
                        observableRead = true;
                        return true;
                    case "Parameter":
                        Add( ref parameters, ReadSiLAElement( r ) );
                        return true;
                    case "Response":
                        Add( ref responses, ReadSiLAElement( r ) );
                        return true;
                    case "IntermediateResponse":
                        Add( ref intermediateResponses, ReadSiLAElement( r ) );
                        return true;
                    case "DefinedExecutionErrors":
                        command.DefinedExecutionErrors = ReadStringArray( r, "Identifier" ) ?? command.DefinedExecutionErrors;
                        return true;
                    default:
                        return false;
                }
            } );
            command.Parameter = ToArray( parameters );
            command.Response = ToArray( responses );
            command.IntermediateResponse = ToArray( intermediateResponses );
            return command;
        }

        private static SiLAElement ReadSiLAElement( XmlReader reader )
        {
            CheckType( reader, nameof( SiLAElement ) );
            var element = new SiLAElement();
            bool identifierRead = false, displayNameRead = false, descriptionRead = false, dataTypeRead = false;
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "Identifier" when !identifierRead:
                        element.Identifier = ReadString( r );
                        identifierRead = true;
                        return true;
                    case "DisplayName" when !displayNameRead:
                        element.DisplayName = ReadString( r );
                        displayNameRead = true;
                        return true;
                    case "Description" when !descriptionRead:
                        element.Description = ReadString( r );
                        descriptionRead = true;
                        return true;
                    case "DataType" when !dataTypeRead:
                        element.DataType = ReadDataTypeType( r );
                        dataTypeRead = true;
                        return true;
                    default:
                        return false;
                }
            } );
            return element;
        }

        private static DataTypeType ReadDataTypeType( XmlReader reader )
        {
            CheckType( reader, nameof( DataTypeType ) );
            var dataType = new DataTypeType();
            var itemRead = false;
            ReadContent( reader, r =>
            {
                if(itemRead || r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "Basic":
                        dataType.Item = ParseBasicType( ReadString( r ) );
                        break;
                    case "Constrained":
                        dataType.Item = ReadConstrainedType( r );
                        break;
                    case "DataTypeIdentifier":
                        dataType.Item = ReadString( r );
                        break;
                    case "List":
                        dataType.Item = ReadListType( r );
                        break;
                    case "Structure":
                        dataType.Item = ReadStructureType( r );
                        break;
                    default:
                        return false;
                }
                itemRead = true;
                return true;
            } );
            return dataType;
        }

        private static ConstrainedType ReadConstrainedType( XmlReader reader )
        {
            CheckType( reader, nameof( ConstrainedType ) );
            var constrained = new ConstrainedType();
            bool dataTypeRead = false, constraintsRead = false;
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "DataType" when !dataTypeRead:
                        constrained.DataType = ReadDataTypeType( r );
                        dataTypeRead = true;
                        return true;
                    case "Constraints" when !constraintsRead:
                        constrained.Constraints = ReadConstraints( r );
                        constraintsRead = true;
                        return true;
                    default:
                        return false;
                }
            } );
            return constrained;
        }

        private static Constraints ReadConstraints( XmlReader reader )
        {
            CheckType( reader, nameof( Constraints ) );
            var constraints = new Constraints();
            var read = new bool[16];
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "Length" when !read[0]:
                        constraints.Length = ReadIntegerString( r );
                        read[0] = true;
                        return true;
                    case "MinimalLength" when !read[1]:
                        constraints.MinimalLength = ReadIntegerString( r );
                        read[1] = true;
                        return true;
                    case "MaximalLength" when !read[2]:
                        constraints.MaximalLength = ReadIntegerString( r );
                        read[2] = true;
                        return true;
                    case "Set":
                        constraints.Set = ReadStringArray( r, "Value" ) ?? constraints.Set;
                        return true;
                    case "Pattern" when !read[4]:
                        constraints.Pattern = ReadString( r );
                        read[4] = true;
                        return true;
                    case "MaximalExclusive" when !read[5]:
                        constraints.MaximalExclusive = ReadString( r );
                        read[5] = true;
                        return true;
                    case "MaximalInclusive" when !read[6]:
                        constraints.MaximalInclusive = ReadString( r );
                        read[6] = true;
                        return true;
                    case "MinimalExclusive" when !read[7]:
                        constraints.MinimalExclusive = ReadString( r );
                        read[7] = true;
                        return true;
                    case "MinimalInclusive" when !read[8]:
                        constraints.MinimalInclusive = ReadString( r );
                        read[8] = true;
                        return true;
                    case "Unit" when !read[9]:
                        constraints.Unit = ReadConstraintsUnit( r );
                        read[9] = true;
                        return true;
                    case "ContentType" when !read[10]:
                        constraints.ContentType = ReadConstraintsContentType( r );
                        read[10] = true;
                        return true;
                    case "ElementCount" when !read[11]:
                        constraints.ElementCount = ReadIntegerString( r );
                        read[11] = true;
                        return true;
                    case "MinimalElementCount" when !read[12]:
                        constraints.MinimalElementCount = ReadIntegerString( r );
                        read[12] = true;
                        return true;
                    case "MaximalElementCount" when !read[13]:
                        constraints.MaximalElementCount = ReadIntegerString( r );
                        read[13] = true;
                        return true;
                    case "FullyQualifiedIdentifier" when !read[14]:
                        constraints.FullyQualifiedIdentifierSpecified = true;
                        constraints.FullyQualifiedIdentifier = ParseConstraintsFullyQualifiedIdentifier( ReadString( r ) );
                        read[14] = true;
                        return true;
                    case "Schema" when !read[15]:
                        constraints.Schema = ReadConstraintsSchema( r );
                        read[15] = true;
                        return true;
                    case "AllowedTypes":
                        constraints.AllowedTypes = ReadAllowedTypes( r ) ?? constraints.AllowedTypes;
                        return true;
                    default:
                        return false;
                }
            } );
            return constraints;
        }

        private static DataTypeType[] ReadAllowedTypes( XmlReader reader )
        {
            if(ReadNull( reader ))
            {
                return null;
            }
            var items = new List<DataTypeType>();
            ReadContent( reader, r =>
            {
                if(IsSilaElement( r, "DataType" ))
                {
                    items.Add( ReadDataTypeType( r ) );
                    return true;
                }
                return false;
            } );
            return items.ToArray();
        }

        private static ConstraintsUnit ReadConstraintsUnit( XmlReader reader )
        {
            CheckType( reader, null );
            var unit = new ConstraintsUnit();
            bool labelRead = false, factorRead = false, offsetRead = false;
            List<ConstraintsUnitUnitComponent> components = null;
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "Label" when !labelRead:
                        unit.Label = ReadString( r );
                        labelRead = true;
                        return true;
                    case "Factor" when !factorRead:
                        unit.Factor = XmlConvert.ToDecimal( ReadString( r ) );
                        factorRead = true;
                        return true;
                    case "Offset" when !offsetRead:
                        unit.Offset = XmlConvert.ToDecimal( ReadString( r ) );
                        offsetRead = true;
                        return true;
                    case "UnitComponent":
                        Add( ref components, ReadConstraintsUnitUnitComponent( r ) );
                        return true;
                    default:
                        return false;
                }
            } );
            unit.UnitComponent = ToArray( components );
            return unit;
        }

        private static ConstraintsUnitUnitComponent ReadConstraintsUnitUnitComponent( XmlReader reader )
        {
            CheckType( reader, null );
            var component = new ConstraintsUnitUnitComponent();
            bool unitRead = false, exponentRead = false;
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "SIUnit" when !unitRead:
                        component.SIUnit = ParseSIUnit( ReadString( r ) );
                        unitRead = true;
                        return true;
                    case "Exponent" when !exponentRead:
                        component.Exponent = ReadIntegerString( r );
                        exponentRead = true;
                        return true;
                    default:
                        return false;
                }
            } );
            return component;
        }

        private static ConstraintsContentType ReadConstraintsContentType( XmlReader reader )
        {
            CheckType( reader, null );
            var contentType = new ConstraintsContentType();
            bool typeRead = false, subtypeRead = false;
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "Type" when !typeRead:
                        contentType.Type = ReadString( r );
                        typeRead = true;
                        return true;
                    case "Subtype" when !subtypeRead:
                        contentType.Subtype = ReadString( r );
                        subtypeRead = true;
                        return true;
                    case "Parameters":
                        contentType.Parameters = ReadContentTypeParameters( r ) ?? contentType.Parameters;
                        return true;
                    default:
                        return false;
                }
            } );
            return contentType;
        }

        private static ConstraintsContentTypeParameter[] ReadContentTypeParameters( XmlReader reader )
        {
            if(ReadNull( reader ))
            {
                return null;
            }
            var items = new List<ConstraintsContentTypeParameter>();
            ReadContent( reader, r =>
            {
                if(IsSilaElement( r, "Parameter" ))
                {
                    items.Add( ReadConstraintsContentTypeParameter( r ) );
                    return true;
                }
                return false;
            } );
            return items.ToArray();
        }

        private static ConstraintsContentTypeParameter ReadConstraintsContentTypeParameter( XmlReader reader )
        {
            CheckType( reader, null );
            var parameter = new ConstraintsContentTypeParameter();
            bool attributeRead = false, valueRead = false;
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "Attribute" when !attributeRead:
                        parameter.Attribute = ReadString( r );
                        attributeRead = true;
                        return true;
                    case "Value" when !valueRead:
                        parameter.Value = ReadString( r );
                        valueRead = true;
                        return true;
                    default:
                        return false;
                }
            } );
            return parameter;
        }

        private static ConstraintsSchema ReadConstraintsSchema( XmlReader reader )
        {
            CheckType( reader, null );
            var schema = new ConstraintsSchema();
            bool typeRead = false, itemRead = false;
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "Type" when !typeRead:
                        schema.Type = ParseConstraintsSchemaType( ReadString( r ) );
                        typeRead = true;
                        return true;
                    case "Inline" when !itemRead:
                        schema.ItemElementName = ItemChoiceType.Inline;
                        schema.Item = ReadString( r );
                        itemRead = true;
                        return true;
                    case "Url" when !itemRead:
                        schema.ItemElementName = ItemChoiceType.Url;
                        schema.Item = ReadString( r );
                        itemRead = true;
                        return true;
                    default:
                        return false;
                }
            } );
            return schema;
        }

        private static StructureType ReadStructureType( XmlReader reader )
        {
            CheckType( reader, nameof( StructureType ) );
            var structure = new StructureType();
            List<SiLAElement> elements = null;
            ReadContent( reader, r =>
            {
                if(IsSilaElement( r, "Element" ))
                {
                    Add( ref elements, ReadSiLAElement( r ) );
                    return true;
                }
                return false;
            } );
            structure.Element = ToArray( elements );
            return structure;
        }

        private static ListType ReadListType( XmlReader reader )
        {
            CheckType( reader, nameof( ListType ) );
            var list = new ListType();
            var dataTypeRead = false;
            ReadContent( reader, r =>
            {
                if(!dataTypeRead && IsSilaElement( r, "DataType" ))
                {
                    list.DataType = ReadDataTypeType( r );
                    dataTypeRead = true;
                    return true;
                }
                return false;
            } );
            return list;
        }

        private static FeatureDefinedExecutionError ReadFeatureDefinedExecutionError( XmlReader reader )
        {
            CheckType( reader, null );
            var error = new FeatureDefinedExecutionError();
            bool identifierRead = false, displayNameRead = false, descriptionRead = false;
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "Identifier" when !identifierRead:
                        error.Identifier = ReadString( r );
                        identifierRead = true;
                        return true;
                    case "DisplayName" when !displayNameRead:
                        error.DisplayName = ReadString( r );
                        displayNameRead = true;
                        return true;
                    case "Description" when !descriptionRead:
                        error.Description = ReadString( r );
                        descriptionRead = true;
                        return true;
                    default:
                        return false;
                }
            } );
            return error;
        }

        private static FeatureMetadata ReadFeatureMetadata( XmlReader reader )
        {
            CheckType( reader, null );
            var metadata = new FeatureMetadata();
            bool identifierRead = false, displayNameRead = false, descriptionRead = false, dataTypeRead = false;
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "Identifier" when !identifierRead:
                        metadata.Identifier = ReadString( r );
                        identifierRead = true;
                        return true;
                    case "DisplayName" when !displayNameRead:
                        metadata.DisplayName = ReadString( r );
                        displayNameRead = true;
                        return true;
                    case "Description" when !descriptionRead:
                        metadata.Description = ReadString( r );
                        descriptionRead = true;
                        return true;
                    case "DataType" when !dataTypeRead:
                        metadata.DataType = ReadDataTypeType( r );
                        dataTypeRead = true;
                        return true;
                    case "DefinedExecutionErrors":
                        metadata.DefinedExecutionErrors = ReadStringArray( r, "Identifier" ) ?? metadata.DefinedExecutionErrors;
                        return true;
                    default:
                        return false;
                }
            } );
            return metadata;
        }

        private static FeatureProperty ReadFeatureProperty( XmlReader reader )
        {
            CheckType( reader, null );
            var property = new FeatureProperty();
            bool identifierRead = false, displayNameRead = false, descriptionRead = false, observableRead = false, dataTypeRead = false;
            ReadContent( reader, r =>
            {
                if(r.NamespaceURI != SilaNamespace)
                {
                    return false;
                }
                switch(r.LocalName)
                {
                    case "Identifier" when !identifierRead:
                        property.Identifier = ReadString( r );
                        identifierRead = true;
                        return true;
                    case "DisplayName" when !displayNameRead:
                        property.DisplayName = ReadString( r );
                        displayNameRead = true;
                        return true;
                    case "Description" when !descriptionRead:
                        property.Description = ReadString( r );
                        descriptionRead = true;
                        return true;
                    case "Observable" when !observableRead:
                        property.Observable = ParseFeaturePropertyObservable( ReadString( r ) );
                        observableRead = true;
                        return true;
                    case "DataType" when !dataTypeRead:
                        property.DataType = ReadDataTypeType( r );
                        dataTypeRead = true;
                        return true;
                    case "DefinedExecutionErrors":
                        property.DefinedExecutionErrors = ReadStringArray( r, "Identifier" ) ?? property.DefinedExecutionErrors;
                        return true;
                    default:
                        return false;
                }
            } );
            return property;
        }

        private static InvalidOperationException UnknownConstant( string value, string typeName )
        {
            return new InvalidOperationException( $"Instance validation error: '{value}' is not a valid value for {typeName}." );
        }

        private static FeatureMaturityLevel ParseFeatureMaturityLevel( string value )
        {
            switch(value)
            {
                case "Draft": return FeatureMaturityLevel.Draft;
                case "Verified": return FeatureMaturityLevel.Verified;
                case "Normative": return FeatureMaturityLevel.Normative;
                default: throw UnknownConstant( value, nameof( FeatureMaturityLevel ) );
            }
        }

        private static FeatureCommandObservable ParseFeatureCommandObservable( string value )
        {
            switch(value)
            {
                case "Yes": return FeatureCommandObservable.Yes;
                case "No": return FeatureCommandObservable.No;
                default: throw UnknownConstant( value, nameof( FeatureCommandObservable ) );
            }
        }

        private static FeaturePropertyObservable ParseFeaturePropertyObservable( string value )
        {
            switch(value)
            {
                case "Yes": return FeaturePropertyObservable.Yes;
                case "No": return FeaturePropertyObservable.No;
                default: throw UnknownConstant( value, nameof( FeaturePropertyObservable ) );
            }
        }

        private static BasicType ParseBasicType( string value )
        {
            switch(value)
            {
                case "String": return BasicType.String;
                case "Integer": return BasicType.Integer;
                case "Real": return BasicType.Real;
                case "Boolean": return BasicType.Boolean;
                case "Binary": return BasicType.Binary;
                case "Date": return BasicType.Date;
                case "Time": return BasicType.Time;
                case "Timestamp": return BasicType.Timestamp;
                case "Any": return BasicType.Any;
                default: throw UnknownConstant( value, nameof( BasicType ) );
            }
        }

        private static ConstraintsUnitUnitComponentSIUnit ParseSIUnit( string value )
        {
            switch(value)
            {
                case "Dimensionless": return ConstraintsUnitUnitComponentSIUnit.Dimensionless;
                case "Meter": return ConstraintsUnitUnitComponentSIUnit.Meter;
                case "Kilogram": return ConstraintsUnitUnitComponentSIUnit.Kilogram;
                case "Second": return ConstraintsUnitUnitComponentSIUnit.Second;
                case "Ampere": return ConstraintsUnitUnitComponentSIUnit.Ampere;
                case "Kelvin": return ConstraintsUnitUnitComponentSIUnit.Kelvin;
                case "Mole": return ConstraintsUnitUnitComponentSIUnit.Mole;
                case "Candela": return ConstraintsUnitUnitComponentSIUnit.Candela;
                default: throw UnknownConstant( value, nameof( ConstraintsUnitUnitComponentSIUnit ) );
            }
        }

        private static ConstraintsFullyQualifiedIdentifier ParseConstraintsFullyQualifiedIdentifier( string value )
        {
            switch(value)
            {
                case "FeatureIdentifier": return ConstraintsFullyQualifiedIdentifier.FeatureIdentifier;
                case "CommandIdentifier": return ConstraintsFullyQualifiedIdentifier.CommandIdentifier;
                case "CommandParameterIdentifier": return ConstraintsFullyQualifiedIdentifier.CommandParameterIdentifier;
                case "CommandResponseIdentifier": return ConstraintsFullyQualifiedIdentifier.CommandResponseIdentifier;
                case "IntermediateCommandResponseIdentifier": return ConstraintsFullyQualifiedIdentifier.IntermediateCommandResponseIdentifier;
                case "DefinedExecutionErrorIdentifier": return ConstraintsFullyQualifiedIdentifier.DefinedExecutionErrorIdentifier;
                case "PropertyIdentifier": return ConstraintsFullyQualifiedIdentifier.PropertyIdentifier;
                case "TypeIdentifier": return ConstraintsFullyQualifiedIdentifier.TypeIdentifier;
                case "MetadataIdentifier": return ConstraintsFullyQualifiedIdentifier.MetadataIdentifier;
                default: throw UnknownConstant( value, nameof( ConstraintsFullyQualifiedIdentifier ) );
            }
        }

        private static ConstraintsSchemaType ParseConstraintsSchemaType( string value )
        {
            switch(value)
            {
                case "Xml": return ConstraintsSchemaType.Xml;
                case "Json": return ConstraintsSchemaType.Json;
                default: throw UnknownConstant( value, nameof( ConstraintsSchemaType ) );
            }
        }
    }
}
