using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization;

namespace Tecan.Sila2
{
    internal static partial class FeatureDefinitionXml
    {
        /// <summary>
        /// The namespace declarations the XmlSerializer adds to the root element, in the order of the current runtime
        /// </summary>
        private static readonly XmlQualifiedName[] DefaultNamespaces = CreateDefaultNamespaces();

        private static XmlQualifiedName[] CreateDefaultNamespaces()
        {
            // XmlSerializer declares these two prefixes when the caller does not provide namespaces.
            // The enumeration order of XmlSerializerNamespaces depends on the runtime, so it is taken from there.
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add( "xsi", XsiNamespace );
            namespaces.Add( "xsd", XsdNamespace );
            return namespaces.ToArray();
        }

        private static void Serialize<T>( XmlWriter writer, string rootName, string rootNamespace, bool rootIsNullable, T value, Action<ModelWriter, T> writeContent ) where T : class
        {
            try
            {
                if(writer.WriteState == WriteState.Start)
                {
                    writer.WriteStartDocument();
                }
                WriteRootStartElement( writer, rootName, rootNamespace );
                if(value == null)
                {
                    if(rootIsNullable)
                    {
                        writer.WriteAttributeString( "nil", XsiNamespace, "true" );
                    }
                }
                else
                {
                    var modelWriter = new ModelWriter( writer );
                    modelWriter.Enter( value );
                    writeContent( modelWriter, value );
                }
                writer.WriteEndElement();
            }
            catch(Exception exception) when(!(exception is OutOfMemoryException))
            {
                throw new InvalidOperationException( "There was an error generating the XML document.", exception );
            }
            writer.Flush();
        }

        /// <summary>
        /// Writes the elements of the model and, like the XmlSerializer, rejects circular references between model objects
        /// </summary>
        private sealed class ModelWriter
        {
            private readonly HashSet<object> _objectsInUse = new HashSet<object>( ReferenceComparer.Instance );
            private readonly XmlWriter _writer;

            public ModelWriter( XmlWriter writer )
            {
                _writer = writer;
            }

            /// <summary>
            /// Marks the given model object as being written
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown if the object is already being written, i.e. the model contains a cycle</exception>
            public void Enter( object value )
            {
                if(!_objectsInUse.Add( value ))
                {
                    throw new InvalidOperationException( $"A circular reference was detected while serializing an object of type {value.GetType().FullName}." );
                }
            }

            public void WriteStartElement( string localName, string ns, object value )
            {
                Enter( value );
                _writer.WriteStartElement( localName, ns );
            }

            public void WriteEndElement( object value )
            {
                _writer.WriteEndElement();
                _objectsInUse.Remove( value );
            }

            public void WriteStartElement( string localName, string ns )
            {
                _writer.WriteStartElement( localName, ns );
            }

            public void WriteEndElement()
            {
                _writer.WriteEndElement();
            }

            public void WriteElementString( string localName, string ns, string value )
            {
                _writer.WriteElementString( localName, ns, value );
            }

            public void WriteAttributeString( string localName, string ns, string value )
            {
                _writer.WriteAttributeString( localName, ns, value );
            }

            public void WriteRaw( string data )
            {
                _writer.WriteRaw( data );
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals( object x, object y )
            {
                return ReferenceEquals( x, y );
            }

            public int GetHashCode( object obj )
            {
                return RuntimeHelpers.GetHashCode( obj );
            }
        }

        private static void WriteRootStartElement( XmlWriter writer, string rootName, string rootNamespace )
        {
            writer.WriteStartElement( null, rootName, rootNamespace );
            foreach(var ns in DefaultNamespaces)
            {
                if(writer.LookupPrefix( ns.Namespace ) == null)
                {
                    writer.WriteAttributeString( "xmlns", ns.Name, null, ns.Namespace );
                }
            }
        }

        private static void WriteStringElement( ModelWriter writer, string localName, string value )
        {
            if(value != null)
            {
                writer.WriteElementString( localName, SilaNamespace, value );
            }
        }

        private static void WriteAttribute( ModelWriter writer, string localName, string value )
        {
            if(value != null)
            {
                writer.WriteAttributeString( localName, string.Empty, value );
            }
        }

        private static void WriteDecimalElement( ModelWriter writer, string localName, decimal value )
        {
            writer.WriteStartElement( localName, SilaNamespace );
            writer.WriteRaw( XmlConvert.ToString( value ) );
            writer.WriteEndElement();
        }

        private static void WriteStringArray( ModelWriter writer, string localName, string itemName, string[] values )
        {
            if(values == null)
            {
                return;
            }
            writer.WriteStartElement( localName, SilaNamespace );
            foreach(var value in values)
            {
                WriteStringElement( writer, itemName, value );
            }
            writer.WriteEndElement();
        }

        private static void WriteElements( ModelWriter writer, string localName, SiLAElement[] elements )
        {
            if(elements == null)
            {
                return;
            }
            foreach(var element in elements)
            {
                WriteSiLAElement( writer, localName, element );
            }
        }

        /// <summary>
        /// Rejects objects of derived types, which the XmlSerializer cannot write without knowing them
        /// </summary>
        private static void CheckExactType<T>( T value )
        {
            if(value.GetType() != typeof( T ))
            {
                throw UnexpectedType( value );
            }
        }

        private static InvalidOperationException UnexpectedType( object value )
        {
            return new InvalidOperationException( $"The type {value.GetType().FullName} was not expected. Use the XmlInclude or SoapInclude attribute to specify types that are not known statically." );
        }

        private static InvalidOperationException InvalidEnumValue( long value, string typeName )
        {
            return new InvalidOperationException( $"Instance validation error: '{value}' is not a valid value for {typeof( FeatureDefinitionXml ).Namespace}.{typeName}." );
        }

        private static void WriteFeature( ModelWriter writer, Feature feature )
        {
            CheckExactType( feature );
            if(feature.Locale != "en-us")
            {
                WriteAttribute( writer, "Locale", feature.Locale );
            }
            WriteAttribute( writer, "SiLA2Version", feature.SiLA2Version );
            WriteAttribute( writer, "FeatureVersion", feature.FeatureVersion );
            if(feature.MaturityLevel != FeatureMaturityLevel.Draft)
            {
                WriteAttribute( writer, "MaturityLevel", ToString( feature.MaturityLevel ) );
            }
            WriteAttribute( writer, "Originator", feature.Originator );
            if(feature.Category != "none")
            {
                WriteAttribute( writer, "Category", feature.Category );
            }
            WriteStringElement( writer, "Identifier", feature.Identifier );
            WriteStringElement( writer, "DisplayName", feature.DisplayName );
            WriteStringElement( writer, "Description", feature.Description );
            if(feature.Items != null)
            {
                foreach(var item in feature.Items)
                {
                    switch(item)
                    {
                        case null:
                            break;
                        case FeatureCommand command:
                            WriteFeatureCommand( writer, command );
                            break;
                        case SiLAElement dataTypeDefinition:
                            WriteSiLAElement( writer, "DataTypeDefinition", dataTypeDefinition );
                            break;
                        case FeatureDefinedExecutionError error:
                            WriteFeatureDefinedExecutionError( writer, error );
                            break;
                        case FeatureMetadata metadata:
                            WriteFeatureMetadata( writer, metadata );
                            break;
                        case FeatureProperty property:
                            WriteFeatureProperty( writer, property );
                            break;
                        default:
                            throw UnexpectedType( item );
                    }
                }
            }
        }

        private static void WriteFeatureCommand( ModelWriter writer, FeatureCommand command )
        {
            CheckExactType( command );
            writer.WriteStartElement( "Command", SilaNamespace, command );
            WriteStringElement( writer, "Identifier", command.Identifier );
            WriteStringElement( writer, "DisplayName", command.DisplayName );
            WriteStringElement( writer, "Description", command.Description );
            WriteStringElement( writer, "Observable", ToString( command.Observable ) );
            WriteElements( writer, "Parameter", command.Parameter );
            WriteElements( writer, "Response", command.Response );
            WriteElements( writer, "IntermediateResponse", command.IntermediateResponse );
            WriteStringArray( writer, "DefinedExecutionErrors", "Identifier", command.DefinedExecutionErrors );
            writer.WriteEndElement( command );
        }

        private static void WriteSiLAElement( ModelWriter writer, string localName, SiLAElement element )
        {
            if(element == null)
            {
                return;
            }
            CheckExactType( element );
            writer.WriteStartElement( localName, SilaNamespace, element );
            WriteStringElement( writer, "Identifier", element.Identifier );
            WriteStringElement( writer, "DisplayName", element.DisplayName );
            WriteStringElement( writer, "Description", element.Description );
            WriteDataTypeElement( writer, "DataType", element.DataType );
            writer.WriteEndElement( element );
        }

        private static void WriteDataTypeElement( ModelWriter writer, string localName, DataTypeType dataType )
        {
            if(dataType == null)
            {
                return;
            }
            CheckExactType( dataType );
            writer.WriteStartElement( localName, SilaNamespace, dataType );
            WriteDataTypeType( writer, dataType );
            writer.WriteEndElement( dataType );
        }

        private static void WriteDataTypeType( ModelWriter writer, DataTypeType dataType )
        {
            CheckExactType( dataType );
            switch(dataType.Item)
            {
                case null:
                    break;
                case BasicType basic:
                    WriteStringElement( writer, "Basic", ToString( basic ) );
                    break;
                case ConstrainedType constrained:
                    CheckExactType( constrained );
                    writer.WriteStartElement( "Constrained", SilaNamespace, constrained );
                    WriteDataTypeElement( writer, "DataType", constrained.DataType );
                    if(constrained.Constraints != null)
                    {
                        CheckExactType( constrained.Constraints );
                        writer.WriteStartElement( "Constraints", SilaNamespace, constrained.Constraints );
                        WriteConstraints( writer, constrained.Constraints );
                        writer.WriteEndElement( constrained.Constraints );
                    }
                    writer.WriteEndElement( constrained );
                    break;
                case string identifier:
                    WriteStringElement( writer, "DataTypeIdentifier", identifier );
                    break;
                case ListType list:
                    CheckExactType( list );
                    writer.WriteStartElement( "List", SilaNamespace, list );
                    WriteDataTypeElement( writer, "DataType", list.DataType );
                    writer.WriteEndElement( list );
                    break;
                case StructureType structure:
                    CheckExactType( structure );
                    writer.WriteStartElement( "Structure", SilaNamespace, structure );
                    WriteElements( writer, "Element", structure.Element );
                    writer.WriteEndElement( structure );
                    break;
                default:
                    throw UnexpectedType( dataType.Item );
            }
        }

        private static void WriteConstraints( ModelWriter writer, Constraints constraints )
        {
            CheckExactType( constraints );
            WriteStringElement( writer, "Length", constraints.Length );
            WriteStringElement( writer, "MinimalLength", constraints.MinimalLength );
            WriteStringElement( writer, "MaximalLength", constraints.MaximalLength );
            WriteStringArray( writer, "Set", "Value", constraints.Set );
            WriteStringElement( writer, "Pattern", constraints.Pattern );
            WriteStringElement( writer, "MaximalExclusive", constraints.MaximalExclusive );
            WriteStringElement( writer, "MaximalInclusive", constraints.MaximalInclusive );
            WriteStringElement( writer, "MinimalExclusive", constraints.MinimalExclusive );
            WriteStringElement( writer, "MinimalInclusive", constraints.MinimalInclusive );
            if(constraints.Unit != null)
            {
                WriteConstraintsUnit( writer, constraints.Unit );
            }
            if(constraints.ContentType != null)
            {
                WriteConstraintsContentType( writer, constraints.ContentType );
            }
            WriteStringElement( writer, "ElementCount", constraints.ElementCount );
            WriteStringElement( writer, "MinimalElementCount", constraints.MinimalElementCount );
            WriteStringElement( writer, "MaximalElementCount", constraints.MaximalElementCount );
            if(constraints.FullyQualifiedIdentifierSpecified)
            {
                WriteStringElement( writer, "FullyQualifiedIdentifier", ToString( constraints.FullyQualifiedIdentifier ) );
            }
            if(constraints.Schema != null)
            {
                WriteConstraintsSchema( writer, constraints.Schema );
            }
            if(constraints.AllowedTypes != null)
            {
                writer.WriteStartElement( "AllowedTypes", SilaNamespace );
                foreach(var allowedType in constraints.AllowedTypes)
                {
                    WriteDataTypeElement( writer, "DataType", allowedType );
                }
                writer.WriteEndElement();
            }
        }

        private static void WriteConstraintsUnit( ModelWriter writer, ConstraintsUnit unit )
        {
            CheckExactType( unit );
            writer.WriteStartElement( "Unit", SilaNamespace, unit );
            WriteStringElement( writer, "Label", unit.Label );
            WriteDecimalElement( writer, "Factor", unit.Factor );
            WriteDecimalElement( writer, "Offset", unit.Offset );
            if(unit.UnitComponent != null)
            {
                foreach(var component in unit.UnitComponent)
                {
                    if(component == null)
                    {
                        continue;
                    }
                    CheckExactType( component );
                    writer.WriteStartElement( "UnitComponent", SilaNamespace, component );
                    WriteStringElement( writer, "SIUnit", ToString( component.SIUnit ) );
                    WriteStringElement( writer, "Exponent", component.Exponent );
                    writer.WriteEndElement( component );
                }
            }
            writer.WriteEndElement( unit );
        }

        private static void WriteConstraintsContentType( ModelWriter writer, ConstraintsContentType contentType )
        {
            CheckExactType( contentType );
            writer.WriteStartElement( "ContentType", SilaNamespace, contentType );
            WriteStringElement( writer, "Type", contentType.Type );
            WriteStringElement( writer, "Subtype", contentType.Subtype );
            if(contentType.Parameters != null)
            {
                writer.WriteStartElement( "Parameters", SilaNamespace );
                foreach(var parameter in contentType.Parameters)
                {
                    if(parameter == null)
                    {
                        continue;
                    }
                    CheckExactType( parameter );
                    writer.WriteStartElement( "Parameter", SilaNamespace, parameter );
                    WriteStringElement( writer, "Attribute", parameter.Attribute );
                    WriteStringElement( writer, "Value", parameter.Value );
                    writer.WriteEndElement( parameter );
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement( contentType );
        }

        private static void WriteConstraintsSchema( ModelWriter writer, ConstraintsSchema schema )
        {
            CheckExactType( schema );
            writer.WriteStartElement( "Schema", SilaNamespace, schema );
            WriteStringElement( writer, "Type", ToString( schema.Type ) );
            if(schema.Item != null)
            {
                switch(schema.ItemElementName)
                {
                    case ItemChoiceType.Inline:
                        WriteStringElement( writer, "Inline", schema.Item );
                        break;
                    case ItemChoiceType.Url:
                        WriteStringElement( writer, "Url", schema.Item );
                        break;
                    default:
                        throw new InvalidOperationException( $"Invalid or missing value of the choice identifier 'ItemElementName' of type '{typeof( ItemChoiceType ).FullName}'." );
                }
            }
            writer.WriteEndElement( schema );
        }

        private static void WriteFeatureDefinedExecutionError( ModelWriter writer, FeatureDefinedExecutionError error )
        {
            CheckExactType( error );
            writer.WriteStartElement( "DefinedExecutionError", SilaNamespace, error );
            WriteStringElement( writer, "Identifier", error.Identifier );
            WriteStringElement( writer, "DisplayName", error.DisplayName );
            WriteStringElement( writer, "Description", error.Description );
            writer.WriteEndElement( error );
        }

        private static void WriteFeatureMetadata( ModelWriter writer, FeatureMetadata metadata )
        {
            CheckExactType( metadata );
            writer.WriteStartElement( "Metadata", SilaNamespace, metadata );
            WriteStringElement( writer, "Identifier", metadata.Identifier );
            WriteStringElement( writer, "DisplayName", metadata.DisplayName );
            WriteStringElement( writer, "Description", metadata.Description );
            WriteDataTypeElement( writer, "DataType", metadata.DataType );
            WriteStringArray( writer, "DefinedExecutionErrors", "Identifier", metadata.DefinedExecutionErrors );
            writer.WriteEndElement( metadata );
        }

        private static void WriteFeatureProperty( ModelWriter writer, FeatureProperty property )
        {
            CheckExactType( property );
            writer.WriteStartElement( "Property", SilaNamespace, property );
            WriteStringElement( writer, "Identifier", property.Identifier );
            WriteStringElement( writer, "DisplayName", property.DisplayName );
            WriteStringElement( writer, "Description", property.Description );
            WriteStringElement( writer, "Observable", ToString( property.Observable ) );
            WriteDataTypeElement( writer, "DataType", property.DataType );
            WriteStringArray( writer, "DefinedExecutionErrors", "Identifier", property.DefinedExecutionErrors );
            writer.WriteEndElement( property );
        }

        private static string ToString( FeatureMaturityLevel value )
        {
            switch(value)
            {
                case FeatureMaturityLevel.Draft: return "Draft";
                case FeatureMaturityLevel.Verified: return "Verified";
                case FeatureMaturityLevel.Normative: return "Normative";
                default: throw InvalidEnumValue( (long)value, nameof( FeatureMaturityLevel ) );
            }
        }

        private static string ToString( FeatureCommandObservable value )
        {
            switch(value)
            {
                case FeatureCommandObservable.Yes: return "Yes";
                case FeatureCommandObservable.No: return "No";
                default: throw InvalidEnumValue( (long)value, nameof( FeatureCommandObservable ) );
            }
        }

        private static string ToString( FeaturePropertyObservable value )
        {
            switch(value)
            {
                case FeaturePropertyObservable.Yes: return "Yes";
                case FeaturePropertyObservable.No: return "No";
                default: throw InvalidEnumValue( (long)value, nameof( FeaturePropertyObservable ) );
            }
        }

        private static string ToString( BasicType value )
        {
            switch(value)
            {
                case BasicType.String: return "String";
                case BasicType.Integer: return "Integer";
                case BasicType.Real: return "Real";
                case BasicType.Boolean: return "Boolean";
                case BasicType.Binary: return "Binary";
                case BasicType.Date: return "Date";
                case BasicType.Time: return "Time";
                case BasicType.Timestamp: return "Timestamp";
                case BasicType.Any: return "Any";
                default: throw InvalidEnumValue( (long)value, nameof( BasicType ) );
            }
        }

        private static string ToString( ConstraintsUnitUnitComponentSIUnit value )
        {
            switch(value)
            {
                case ConstraintsUnitUnitComponentSIUnit.Dimensionless: return "Dimensionless";
                case ConstraintsUnitUnitComponentSIUnit.Meter: return "Meter";
                case ConstraintsUnitUnitComponentSIUnit.Kilogram: return "Kilogram";
                case ConstraintsUnitUnitComponentSIUnit.Second: return "Second";
                case ConstraintsUnitUnitComponentSIUnit.Ampere: return "Ampere";
                case ConstraintsUnitUnitComponentSIUnit.Kelvin: return "Kelvin";
                case ConstraintsUnitUnitComponentSIUnit.Mole: return "Mole";
                case ConstraintsUnitUnitComponentSIUnit.Candela: return "Candela";
                default: throw InvalidEnumValue( (long)value, nameof( ConstraintsUnitUnitComponentSIUnit ) );
            }
        }

        private static string ToString( ConstraintsFullyQualifiedIdentifier value )
        {
            switch(value)
            {
                case ConstraintsFullyQualifiedIdentifier.FeatureIdentifier: return "FeatureIdentifier";
                case ConstraintsFullyQualifiedIdentifier.CommandIdentifier: return "CommandIdentifier";
                case ConstraintsFullyQualifiedIdentifier.CommandParameterIdentifier: return "CommandParameterIdentifier";
                case ConstraintsFullyQualifiedIdentifier.CommandResponseIdentifier: return "CommandResponseIdentifier";
                case ConstraintsFullyQualifiedIdentifier.IntermediateCommandResponseIdentifier: return "IntermediateCommandResponseIdentifier";
                case ConstraintsFullyQualifiedIdentifier.DefinedExecutionErrorIdentifier: return "DefinedExecutionErrorIdentifier";
                case ConstraintsFullyQualifiedIdentifier.PropertyIdentifier: return "PropertyIdentifier";
                case ConstraintsFullyQualifiedIdentifier.TypeIdentifier: return "TypeIdentifier";
                case ConstraintsFullyQualifiedIdentifier.MetadataIdentifier: return "MetadataIdentifier";
                default: throw InvalidEnumValue( (long)value, nameof( ConstraintsFullyQualifiedIdentifier ) );
            }
        }

        private static string ToString( ConstraintsSchemaType value )
        {
            switch(value)
            {
                case ConstraintsSchemaType.Xml: return "Xml";
                case ConstraintsSchemaType.Json: return "Json";
                default: throw InvalidEnumValue( (long)value, nameof( ConstraintsSchemaType ) );
            }
        }
    }
}
