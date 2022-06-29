using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// A class that serializes/deserializes dynamic objects to byte arrays
    /// </summary>
    public class DynamicObjectSerializer
    {
        /// <summary>
        /// Gets a function used to resolve data types identified by name
        /// </summary>
        public Func<string, DataTypeType> DataTypeResolver { get; }

        /// <summary>
        /// Creates a new dynamic object serializer
        /// </summary>
        /// <param name="dataTypeResolver">a function used to resolve data types identified by name</param>
        public DynamicObjectSerializer( Func<string, DataTypeType> dataTypeResolver )
        {
            DataTypeResolver = dataTypeResolver;
        }

        /// <summary>
        /// Deserializes the given byte array into the provided dynamic property
        /// </summary>
        /// <param name="property">The property the content should be deserialized into</param>
        /// <param name="data">The actual payload data</param>
        /// <param name="readFieldHeader">True, if field headers should be read, otherwise False</param>
        /// <param name="binaryStore">The binary store to resolve binary storage identifiers</param>
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="property"/> is null.</exception>
        public void Deserialize( DynamicObjectProperty property, byte[] data, bool readFieldHeader, IBinaryStore binaryStore )
        {
            if(property == null) throw new ArgumentNullException( nameof( property ) );

            using(var ms = new MemoryStream( data ))
            {
                using(var reader = ProtoReader.Create( ms, null ))
                {
                    if(readFieldHeader && !(property.Type.Item is ListType))
                    {
                        reader.ReadFieldHeader();
                        var token = ProtoReader.StartSubItem( reader );
                        Read( reader, new Action<object>( property.SetValue ), binaryStore, (dynamic)property.Type.Item );
                        ProtoReader.EndSubItem( token, reader );
                    }
                    else
                    {
                        Read( reader, new Action<object>( property.SetValue ), binaryStore, (dynamic)property.Type.Item );
                    }
                }
            }
        }

        private void Read( ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, BasicType type )
        {
            switch(type)
            {
                case BasicType.Any:
                    ReadAny( reader, setProperty, binaryStore );
                    break;
                case BasicType.String:
                    if(IsInField( reader, 1 ))
                    {
                        setProperty( reader.ReadString() );
                    }
                    break;
                case BasicType.Integer:
                    setProperty( IsInField( reader, 1 ) ? reader.ReadInt64() : 0L );
                    break;
                case BasicType.Real:
                    setProperty( IsInField( reader, 1 ) ? reader.ReadDouble() : 0.0 );
                    break; ;
                case BasicType.Boolean:
                    setProperty( IsInField( reader, 1 ) && reader.ReadBoolean() );
                    break;
                case BasicType.Time:
                    ReadTime( reader, setProperty );
                    break;
                case BasicType.Binary:
                    if(IsInField( reader, 1 ))
                    {
                        setProperty( new MemoryStream( Encoding.UTF8.GetBytes( reader.ReadString() ) ) );
                    }
                    else if(IsInField( reader, 2 ))
                    {
                        setProperty( new LazyBinaryStream( binaryStore, reader.ReadString() ) );
                    }
                    break;
                case BasicType.Date:
                    ReadDate( reader, setProperty );
                    break;
                case BasicType.Timestamp:
                    ReadTimestamp( reader, setProperty );
                    break;
                default:
                    throw new ArgumentOutOfRangeException( nameof( type ), type, $"Type {type} is not a supported basic type." );
            }
        }

        private static int ReadIntOfField( ProtoReader reader, int field )
        {
            if(IsInField( reader, field ))
            {
                return (int)reader.ReadUInt32();
            }

            return 0;
        }

        private void ReadAny( ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore )
        {
            if(IsInField( reader, 1 ))
            {
                var type = reader.ReadString();
                var actualType = DataTypeSerializer.LoadType( type );
                var result = new DynamicObjectProperty( "Runtime", null, null, actualType );
                if(IsInField( reader, 2 ))
                {
                    var token = ProtoReader.StartSubItem( reader );
                    var setValue = new Action<object>( result.SetValue );
                    Read( reader, setValue, binaryStore, (dynamic)(actualType.Item) );
                    ProtoReader.EndSubItem( token, reader );
                    setProperty( result );
                }
            }
        }

        private static void ReadTimestamp( ProtoReader reader, Action<object> setProperty )
        {
            var seconds = ReadIntOfField( reader, 1 );
            var minute = ReadIntOfField( reader, 2 );
            var hour = ReadIntOfField( reader, 3 );
            var day = ReadIntOfField( reader, 4 );
            var month = ReadIntOfField( reader, 5 );
            var year = ReadIntOfField( reader, 6 );
            var timeZone = ReadTimezone( reader, 7 );
            setProperty( new DateTimeOffset( year, month, day, hour, minute, seconds, timeZone ) );
        }

        private static void ReadDate( ProtoReader reader, Action<object> setProperty )
        {
            var day = ReadIntOfField( reader, 1 );
            var month = ReadIntOfField( reader, 2 );
            var year = ReadIntOfField( reader, 3 );
            var timeZone = ReadTimezone( reader, 4 );
            setProperty( new DateTimeOffset( year, month, day, 0, 0, 0, timeZone ) );
        }

        private static void ReadTime( ProtoReader reader, Action<object> setProperty )
        {
            var seconds = ReadIntOfField( reader, 1 );
            var minute = ReadIntOfField( reader, 2 );
            var hour = ReadIntOfField( reader, 3 );

            var time = new TimeSpan( hour, minute, seconds );
            var timeZone = ReadTimezone( reader, 4 );

            setProperty( time + timeZone );
        }

        private static TimeSpan ReadTimezone( ProtoReader reader, int field )
        {
            if(!IsInField( reader, field ))
            {
                return TimeSpan.Zero;
            }
            var innerToken = ProtoReader.StartSubItem( reader );
            var timeZoneHour = 0;
            if(IsInField( reader, 1 ))
            {
                timeZoneHour = reader.ReadInt32();
            }

            var timeZoneMinute = ReadIntOfField( reader, 2 );
            ProtoReader.EndSubItem( innerToken, reader );
            var timeZone = new TimeSpan( timeZoneHour, timeZoneMinute, 0 );
            return timeZone;

        }

        private static bool IsInField( ProtoReader reader, int field )
        {
            return reader.TryReadFieldHeader( field );
        }

        private void Read( ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, ListType type )
        {
            var readSubItems = false;
            SubItemToken token = default;
            if(reader.Position == 0)
            {
                readSubItems = true;
                reader.ReadFieldHeader();
                token = ProtoReader.StartSubItem( reader );
            }
            var field = reader.FieldNumber;
            var list = CreateList( type.DataType ) ?? new ObservableCollection<object>();
            var action = new Action<object>( list.Add );
            Read( reader, action, binaryStore, (dynamic)type.DataType.Item );
            if(readSubItems)
            {
                ProtoReader.EndSubItem( token, reader );
            }
            while(reader.TryReadFieldHeader( field ))
            {
                if(readSubItems)
                {
                    token = ProtoReader.StartSubItem( reader );
                }
                Read( reader, action, binaryStore, (dynamic)type.DataType.Item );

                if(readSubItems)
                {
                    ProtoReader.EndSubItem( token, reader );
                }
            }

            setProperty( list );
        }

        /// <summary>
        /// Creates a new collection of the given SiLA2 element type
        /// </summary>
        /// <param name="elementType">The element type</param>
        /// <returns></returns>
        protected virtual ICollection<object> CreateList( DataTypeType elementType )
        {
            return null;
        }

        private void Read( ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, StructureType type )
        {
            int field;
            var dynamicObject = new DynamicObject();
            var lastField = -1;
            DynamicObjectProperty lastProperty = null;
            while((field = reader.ReadFieldHeader()) > 0)
            {
                if(field <= type.Element.Length)
                {
                    var element = type.Element[field - 1];
                    var fieldType = element.DataType;
                    Action<object> readAction;
                    if(field == lastField)
                    {
                        readAction = lastProperty.MergeValue;
                    }
                    else
                    {
                        var child = new DynamicObjectProperty( element );
                        dynamicObject.Elements.Add( child );
                        readAction = child.SetValue;
                        lastField = field;
                        lastProperty = child;
                    }
                    var token = ProtoReader.StartSubItem( reader );
                    Read( reader, new Action<object>( readAction ), binaryStore, (dynamic)(fieldType.Item) );
                    ProtoReader.EndSubItem( token, reader );
                }
                else
                {
                    reader.SkipField();
                }
            }

            if(dynamicObject.Elements.Count > 0)
            {
                setProperty( dynamicObject );
            }
        }

        private void Read( ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, ConstrainedType type )
        {
            Read( reader, setProperty, binaryStore, (dynamic)(type.DataType.Item) );
        }

        private void Read( ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, string typeName )
        {
            reader.ReadFieldHeader();
            var type = DataTypeResolver?.Invoke( typeName )?.Item;
            var token = ProtoReader.StartSubItem( reader );
            Read( reader, setProperty, binaryStore, (dynamic)(type) );
            ProtoReader.EndSubItem( token, reader );
        }

        /// <summary>
        /// Serializes the given dynamic property into a byte array
        /// </summary>
        /// <param name="property">The dynamic property</param>
        /// <param name="store">A component that handles binary transfer</param>
        /// <returns>A byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="property"/> is null.</exception>
        public byte[] Serialize( DynamicObjectProperty property, IBinaryStore store )
        {
            if(property == null) throw new ArgumentNullException( nameof( property ) );

            using(var ms = new MemoryStream())
            {
                using(var writer = ProtoWriter.Create( ms, null ))
                {
                    Write( writer, property.Value, 0, store, (dynamic)property.Type.Item );
                    writer.Close();
                    return ms.ToArray();
                }
            }
        }

        private void Write( ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, BasicType type )
        {
            SubItemToken token = default;
            if(fieldNumber > 0)
            {
                ProtoWriter.WriteFieldHeader( fieldNumber, WireType.String, writer );
                token = ProtoWriter.StartSubItem( value, writer );
            }
            switch(type)
            {
                case BasicType.String:
                    ProtoWriter.WriteFieldHeader( 1, WireType.String, writer );
                    ProtoWriter.WriteString( (string)value, writer );
                    break;
                case BasicType.Integer:
                    ProtoWriter.WriteFieldHeader( 1, WireType.Variant, writer );
                    ProtoWriter.WriteInt64( (long)value, writer );
                    break;
                case BasicType.Real:
                    ProtoWriter.WriteFieldHeader( 1, WireType.Fixed64, writer );
                    ProtoWriter.WriteDouble( (double)value, writer );
                    break;
                case BasicType.Boolean:
                    ProtoWriter.WriteFieldHeader( 1, WireType.Variant, writer );
                    ProtoWriter.WriteBoolean( (bool)value, writer );
                    break;
                case BasicType.Time:
                    WriteTime( writer, value );
                    break;
                case BasicType.Binary:
                    WriteBinary( writer, value, store );
                    break;
                case BasicType.Date:
                    WriteDate( writer, value );
                    break;
                case BasicType.Timestamp:
                    WriteTimestamp( writer, value );
                    break;
                case BasicType.Any:
                    WriteAny( writer, value, store );
                    break;
                default:
                    throw new ArgumentOutOfRangeException( nameof( type ), type, $"Type {type} is not a supported basic type" );
            }
            if(fieldNumber > 0)
            {
                ProtoWriter.EndSubItem( token, writer );
            }
        }

        private void WriteAny( ProtoWriter writer, object value, IBinaryStore store )
        {
            if(value is DynamicObjectProperty property)
            {
                ProtoWriter.WriteFieldHeader( 1, WireType.String, writer );
                ProtoWriter.WriteString( DataTypeSerializer.SaveToString( property.Type ), writer );
                ProtoWriter.WriteFieldHeader( 2, WireType.String, writer );
                var payload = AnyTypeDto.Serializer.Serialize( property, store );
                ProtoWriter.WriteBytes( payload, writer );
            }
        }

        private static void WriteBinary( ProtoWriter writer, object value, IBinaryStore store )
        {
            switch(value)
            {
                case Stream stream:
                    if(stream.Length > 0 && store.ShouldStoreBinary( stream.Length ))
                    {
                        WriteBinaryIdentifier( writer, store.StoreBinary( stream ) );
                    }
                    else
                    {
                        using(var ms = new MemoryStream())
                        {
                            stream.CopyTo( ms );
                            WriteBinaryData( writer, ms.ToArray() );
                        }
                    }
                    return;
                case byte[] data:
                    if(store.ShouldStoreBinary( data.LongLength ))
                    {
                        using(var ms = new MemoryStream( data ))
                        {
                            WriteBinaryIdentifier( writer, store.StoreBinary( ms ) );
                        }
                    }
                    else
                    {
                        WriteBinaryData( writer, data );
                    }
                    return;
                case FileInfo fileInfo:
                    WriteBinaryIdentifier( writer, store.StoreBinary( fileInfo ) );
                    return;
                default: throw new InvalidOperationException( $"Cannot transfer {value} as binary, need Byte array, Stream or FileInfo." );
            }
        }

        private static void WriteBinaryData( ProtoWriter writer, byte[] data )
        {
            ProtoWriter.WriteFieldHeader( 1, WireType.String, writer );
            ProtoWriter.WriteBytes( data, writer );
        }

        private static void WriteBinaryIdentifier( ProtoWriter writer, string identifier )
        {
            ProtoWriter.WriteFieldHeader( 2, WireType.String, writer );
            ProtoWriter.WriteString( identifier, writer );
        }

        private static void WriteTimestamp( ProtoWriter writer, object value )
        {
            var dateTimeOffset = GetDateTimeOffset( value );
            ProtoWriter.WriteFieldHeader( 1, WireType.Variant, writer );
            ProtoWriter.WriteUInt32( (uint)dateTimeOffset.Second, writer );
            ProtoWriter.WriteFieldHeader( 2, WireType.Variant, writer );
            ProtoWriter.WriteUInt32( (uint)dateTimeOffset.Minute, writer );
            ProtoWriter.WriteFieldHeader( 3, WireType.Variant, writer );
            ProtoWriter.WriteUInt32( (uint)dateTimeOffset.Hour, writer );
            ProtoWriter.WriteFieldHeader( 4, WireType.Variant, writer );
            ProtoWriter.WriteUInt32( (uint)dateTimeOffset.Day, writer );
            ProtoWriter.WriteFieldHeader( 5, WireType.Variant, writer );
            ProtoWriter.WriteUInt32( (uint)dateTimeOffset.Month, writer );
            ProtoWriter.WriteFieldHeader( 6, WireType.Variant, writer );
            ProtoWriter.WriteUInt32( (uint)dateTimeOffset.Year, writer );
            ProtoWriter.WriteFieldHeader( 7, WireType.String, writer );
            var subToken = ProtoWriter.StartSubItem( dateTimeOffset, writer );
            if(dateTimeOffset.Offset.Hours != 0)
            {
                ProtoWriter.WriteFieldHeader( 1, WireType.Variant, writer );
                ProtoWriter.WriteInt32( dateTimeOffset.Offset.Hours, writer );
            }
            if(dateTimeOffset.Offset.Minutes != 0)
            {
                ProtoWriter.WriteFieldHeader( 2, WireType.Variant, writer );
                ProtoWriter.WriteInt32( dateTimeOffset.Offset.Minutes, writer );
            }
            ProtoWriter.EndSubItem( subToken, writer );
        }

        private static void WriteDate( ProtoWriter writer, object value )
        {
            DateTimeOffset date = GetDateTimeOffset( value );
            ProtoWriter.WriteFieldHeader( 1, WireType.Variant, writer );
            ProtoWriter.WriteUInt32( (uint)date.Day, writer );
            ProtoWriter.WriteFieldHeader( 2, WireType.Variant, writer );
            ProtoWriter.WriteUInt32( (uint)date.Month, writer );
            ProtoWriter.WriteFieldHeader( 3, WireType.Variant, writer );
            ProtoWriter.WriteUInt32( (uint)date.Year, writer );
            ProtoWriter.WriteFieldHeader( 4, WireType.String, writer );
            var subToken = ProtoWriter.StartSubItem( date, writer );
            if(date.Offset.Hours != 0)
            {
                ProtoWriter.WriteFieldHeader( 1, WireType.Variant, writer );
                ProtoWriter.WriteInt32( date.Offset.Hours, writer );
            }
            if(date.Offset.Minutes != 0)
            {
                ProtoWriter.WriteFieldHeader( 2, WireType.Variant, writer );
                ProtoWriter.WriteInt32( date.Offset.Minutes, writer );
            }
            ProtoWriter.EndSubItem( subToken, writer );
        }

        private static DateTimeOffset GetDateTimeOffset( object value )
        {
            switch(value)
            {
                case DateTime dateTime:
                    return new DateTimeOffset( dateTime, TimeSpan.Zero );
                case DateTimeOffset dateTimeOffset:
                    return dateTimeOffset;
                default:
                    throw new NotSupportedException( "Date or timestamp values must be either DateTime or DateTimeOffset" );
            }
        }

        private static void WriteTime( ProtoWriter writer, object value )
        {
            var timeSpan = (TimeSpan)value;
            ProtoWriter.WriteFieldHeader( 1, WireType.Variant, writer );
            ProtoWriter.WriteUInt32( (uint)timeSpan.Seconds, writer );
            ProtoWriter.WriteFieldHeader( 2, WireType.Variant, writer );
            ProtoWriter.WriteUInt32( (uint)timeSpan.Minutes, writer );
            ProtoWriter.WriteFieldHeader( 3, WireType.Variant, writer );
            ProtoWriter.WriteUInt32( (uint)timeSpan.Hours, writer );
            ProtoWriter.WriteFieldHeader( 4, WireType.String, writer );
            var subToken = ProtoWriter.StartSubItem( timeSpan, writer );
            ProtoWriter.EndSubItem( subToken, writer );
        }

        private void Write( ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, StructureType type )
        {
            if(!(value is DynamicObject dynamicObject))
            {
                if(value != null)
                {
                    throw new InvalidOperationException( $"Cannot write {value} as structured type. Expecting Dynamic Object instead." );
                }

                return;
            }
            var token = default( SubItemToken );
            if(fieldNumber > 0)
            {
                ProtoWriter.WriteFieldHeader( fieldNumber, WireType.String, writer );
                token = ProtoWriter.StartSubItem( dynamicObject, writer );
            }

            if(type.Element != null)
            {
                for(var i = 0; i < type.Element.Length; i++)
                {
                    var element = type.Element[i];
                    var prop = dynamicObject.Elements.FirstOrDefault( p => p.Identifier == element.Identifier );
                    if(prop != null)
                    {
                        Write( writer, prop.Value, i + 1, store, (dynamic)element.DataType.Item );
                    }
                }
            }

            if(fieldNumber > 0)
            {
                ProtoWriter.EndSubItem( token, writer );
            }
        }

        private void Write( ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, ListType type )
        {
            if(fieldNumber == 0)
            {
                fieldNumber = 1;
            }
            if(value is IEnumerable collection)
            {
                foreach(var element in collection)
                {
                    Write( writer, element, fieldNumber, store, (dynamic)type.DataType.Item );
                }
            }
        }

        private void Write( ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, ConstrainedType type )
        {
            Write( writer, value, fieldNumber, store, (dynamic)type.DataType.Item );
        }

        private void Write( ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, string typeName )
        {
            var type = DataTypeResolver?.Invoke( typeName )?.Item;
            if(fieldNumber == 0)
            {
                Write( writer, value, 1, store, (dynamic)type );
            }
            else
            {
                ProtoWriter.WriteFieldHeader( fieldNumber, WireType.String, writer );
                var token = ProtoWriter.StartSubItem( value, writer );
                Write( writer, value, 1, store, (dynamic)type );
                ProtoWriter.EndSubItem( token, writer );
            }
        }
    }
}
