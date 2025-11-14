using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
        /// <param name="encapsulated">True, if the ProtoBuf object is wrapped, otherwise False</param>
        /// <param name="binaryStore">The binary store to resolve binary storage identifiers</param>
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="property"/> is null.</exception>
        public void Deserialize( DynamicObjectProperty property, byte[] data, bool encapsulated, IBinaryStore binaryStore )
        {
            if(property == null) throw new ArgumentNullException( nameof( property ) );

            if (data == null)
            {
                return;
            }

            using(var ms = new MemoryStream( data ))
            {
                using (var reader = ProtoReader.Create(ms, null))
                {
                    if (reader.ReadFieldHeader() != 0)
                    {
                        Read(reader, new Action<object>(property.SetValue), binaryStore, property.Type, encapsulated);
                    }
                }
            }
        }

        private void Read(ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, DataTypeType type, bool encapsulated)
        {
            switch (type.Item) 
            {
                case BasicType basicType:
                    if (encapsulated)
                    {
                        var token = ProtoReader.StartSubItem(reader);
                        if (reader.ReadFieldHeader() != 0)
                        {
                            Read(reader, setProperty, binaryStore, basicType);
                        }
                        ProtoReader.EndSubItem(token, reader);
                    }
                    else
                    {
                        Read(reader, setProperty, binaryStore, basicType);
                    }
                    break;
                case ListType listType:
                    Read(reader, setProperty, binaryStore, listType ); break;
                case ConstrainedType constrainedType:
                    Read(reader, setProperty, binaryStore, constrainedType, encapsulated ); break;
                case StructureType structureType:
                    if (encapsulated)
                    {
                        var token = ProtoReader.StartSubItem(reader);
                        if (reader.ReadFieldHeader() != 0)
                        {
                            Read(reader, setProperty, binaryStore, structureType);
                        }
                        ProtoReader.EndSubItem(token, reader);
                    }
                    else
                    {
                        Read(reader, setProperty, binaryStore, structureType);
                    }
                    break;
                case string identifier:
                    if (encapsulated)
                    {
                        var token = ProtoReader.StartSubItem(reader);
                        if (reader.ReadFieldHeader() != 0)
                        {
                            Read(reader, setProperty, binaryStore, identifier);
                        }
                        ProtoReader.EndSubItem(token, reader);
                    }
                    else
                    {
                        Read(reader, setProperty, binaryStore, identifier);
                    } 
                    break;
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
                    if(reader.FieldNumber == 1)
                    {
                        setProperty( reader.ReadString() );
                    }
                    break;
                case BasicType.Integer:
                    setProperty(reader.FieldNumber == 1 ? reader.ReadInt64() : 0L );
                    break;
                case BasicType.Real:
                    setProperty(reader.FieldNumber == 1 ? reader.ReadDouble() : 0.0 );
                    break; ;
                case BasicType.Boolean:
                    setProperty(reader.FieldNumber == 1 && reader.ReadBoolean() );
                    break;
                case BasicType.Time:
                    ReadTime( reader, setProperty );
                    break;
                case BasicType.Binary:
                    if(reader.FieldNumber == 1)
                    {
                        setProperty( new MemoryStream( ProtoReader.AppendBytes(null, reader) ) );
                    }
                    else if(reader.FieldNumber == 2)
                    {
                        setProperty( new LazyBinaryStream( binaryStore, reader.ReadString() ) );
                    }
                    else
                    {
                        reader.SkipField();
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
            if (reader.FieldNumber == field)
            {
                return (int)reader.ReadUInt32();
            }
            else if(reader.FieldNumber < field && MoveToField( reader, field ))
            {
                return (int)reader.ReadUInt32();
            }

            return 0;
        }

        private void ReadAny( ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore )
        {
            if(reader.FieldNumber == 1)
            {
                var type = reader.ReadString();
                var actualType = DataTypeSerializer.LoadType( type );
                var result = new DynamicObjectProperty( "Runtime", null, null, actualType );
                if (MoveToField(reader, 2))
                {
                    var innerToken = ProtoReader.StartSubItem(reader);
                    if (reader.ReadFieldHeader() != 0)
                    {
                        var setValue = new Action<object>(result.SetValue);
                        Read(reader, setValue, binaryStore, actualType, true);
                        setProperty(result);
                    }
                    ProtoReader.EndSubItem(innerToken, reader);
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
            var millisecond = ReadIntOfField( reader, 8 );
            setProperty( new DateTimeOffset( year, month, day, hour, minute, seconds, millisecond, timeZone ) );
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

            var timeZone = ReadTimezone( reader, 4 );
            var millisecond = ReadIntOfField( reader, 5 );
            var time = new TimeSpan( 0, hour, minute, seconds, millisecond );

            setProperty( time + timeZone );
        }

        private static TimeSpan ReadTimezone( ProtoReader reader, int field )
        {
            if(reader.FieldNumber < field && !MoveToField( reader, field ))
            {
                return TimeSpan.Zero;
            }
            var innerToken = ProtoReader.StartSubItem( reader );
            var timeZoneHour = 0;
            if(MoveToField( reader, 1 ))
            {
                timeZoneHour = reader.ReadInt32();
            }
            var timeZoneMinute = ReadIntOfField( reader, 2 );
            ProtoReader.EndSubItem( innerToken, reader );
            var timeZone = new TimeSpan( timeZoneHour, timeZoneMinute, 0 );
            return timeZone;

        }

        private static bool MoveToField( ProtoReader reader, int field )
        {
            return reader.TryReadFieldHeader( field );
        }

        private void Read( ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, ListType type )
        {
            var field = reader.FieldNumber;
            var list = CreateList( type.DataType ) ?? new ObservableCollection<object>();
            var action = new Action<object>(list.Add);
            Read(reader, action, binaryStore, type.DataType, true);
            while (reader.TryReadFieldHeader(field))
            {
                Read(reader, action, binaryStore, type.DataType, true);
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
            int field = reader.FieldNumber;
            var dynamicObject = new DynamicObject();
            var lastField = -1;
            DynamicObjectProperty lastProperty = null;
            while(field > 0)
            {
                if(field <= type.Element.Length)
                {
                    var element = type.Element[field - 1];
                    var fieldType = element.DataType;
                    Action<object> readAction;
                    if(field == lastField)
                    {
#pragma warning disable S2259 // Null pointers should not be dereferenced
                        readAction = lastProperty.MergeValue;
#pragma warning restore S2259 // Null pointers should not be dereferenced
                    }
                    else
                    {
                        var child = new DynamicObjectProperty( element );
                        dynamicObject.Elements.Add( child );
                        readAction = child.SetValue;
                        lastField = field;
                        lastProperty = child;
                    }
                    Read( reader, new Action<object>( readAction ), binaryStore, fieldType, true );
                }
                else
                {
                    reader.SkipField();
                }
                field = reader.ReadFieldHeader();
            }

            if(dynamicObject.Elements.Count > 0)
            {
                setProperty( dynamicObject );
            }
        }

        private void Read( ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, ConstrainedType type, bool encapsulated )
        {
            Read( reader, setProperty, binaryStore, type.DataType, encapsulated );
        }

        private void Read( ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, string typeName )
        {
            var type = DataTypeResolver?.Invoke( typeName );
            var token = ProtoReader.StartSubItem( reader );
            if (reader.ReadFieldHeader() != 0)
            {
                Read(reader, setProperty, binaryStore, type, false);
            }
            ProtoReader.EndSubItem( token, reader );
        }


        /// <summary>
        /// Serializes the given dynamic property into a byte array
        /// </summary>
        /// <param name="property">The dynamic property</param>
        /// <param name="store">A component that handles binary transfer</param>
        /// <returns>A byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="property"/> is null.</exception>
        public byte[] Serialize(DynamicObjectProperty property, IBinaryStore store) => Serialize(property, store, 0);

        /// <summary>
        /// Serializes the given dynamic property into a byte array
        /// </summary>
        /// <param name="property">The dynamic property</param>
        /// <param name="store">A component that handles binary transfer</param>
        /// <returns>A byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="property"/> is null.</exception>
        public byte[] Serialize(DynamicObjectProperty property, IBinaryStore store, int fieldNumber)
        {
            if(property == null) throw new ArgumentNullException( nameof( property ) );

            using(var ms = new MemoryStream())
            {
                using(var writer = ProtoWriter.Create( ms, null ))
                {
                    try
                    {
                        Write(writer, property.Value, fieldNumber, store, property.Type);  
                        writer.Close();
                    }
                    catch
                    {
                        writer.Abandon();
                        throw;
                    }
                    return ms.ToArray();
                }
            }
        }

        private void Write(ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, DataTypeType type)
        {
            switch (type.Item)
            {
                case BasicType basicType:
                    Write(writer, value, fieldNumber, store, basicType); break;
                case ListType listType:
                    Write(writer, value, fieldNumber, store, listType); break;
                case ConstrainedType constrainedType:
                    Write(writer, value, fieldNumber, store, constrainedType); break;
                case StructureType structureType:
                    Write(writer, value, fieldNumber, store, structureType); break;
                case string identifier:
                    Write(writer, value, fieldNumber, store, identifier); break;
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
                    if (value != null)
                    {
                        ProtoWriter.WriteFieldHeader(1, WireType.String, writer);
                        ProtoWriter.WriteString((string)value, writer);
                    }
                    break;
                case BasicType.Integer:
                    ProtoWriter.WriteFieldHeader( 1, WireType.Varint, writer );
                    ProtoWriter.WriteInt64( Convert.ToInt64( value, CultureInfo.InvariantCulture), writer );
                    break;
                case BasicType.Real:
                    ProtoWriter.WriteFieldHeader( 1, WireType.Fixed64, writer );
                    ProtoWriter.WriteDouble( Convert.ToDouble(value, CultureInfo.InvariantCulture), writer );
                    break;
                case BasicType.Boolean:
                    ProtoWriter.WriteFieldHeader( 1, WireType.Varint, writer );
                    ProtoWriter.WriteBoolean( Convert.ToBoolean(value, CultureInfo.InvariantCulture), writer );
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
                ProtoWriter.WriteFieldHeader(2, WireType.String, writer);
                var payload = AnyTypeDto.Serializer.Serialize( property, store, 1 );
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
            ProtoWriter.WriteFieldHeader( 1, WireType.Varint, writer );
            ProtoWriter.WriteUInt32( (uint)dateTimeOffset.Second, writer );
            ProtoWriter.WriteFieldHeader( 2, WireType.Varint, writer );
            ProtoWriter.WriteUInt32( (uint)dateTimeOffset.Minute, writer );
            ProtoWriter.WriteFieldHeader( 3, WireType.Varint, writer );
            ProtoWriter.WriteUInt32( (uint)dateTimeOffset.Hour, writer );
            ProtoWriter.WriteFieldHeader( 4, WireType.Varint, writer );
            ProtoWriter.WriteUInt32( (uint)dateTimeOffset.Day, writer );
            ProtoWriter.WriteFieldHeader( 5, WireType.Varint, writer );
            ProtoWriter.WriteUInt32( (uint)dateTimeOffset.Month, writer );
            ProtoWriter.WriteFieldHeader( 6, WireType.Varint, writer );
            ProtoWriter.WriteUInt32( (uint)dateTimeOffset.Year, writer );
            ProtoWriter.WriteFieldHeader( 7, WireType.String, writer );
            WriteOffset(writer, dateTimeOffset.Offset);
            ProtoWriter.WriteFieldHeader(8, WireType.Varint, writer);
            ProtoWriter.WriteUInt32((uint)dateTimeOffset.Millisecond, writer);
        }

        private static void WriteDate( ProtoWriter writer, object value )
        {
            DateTimeOffset date = GetDateTimeOffset( value );
            ProtoWriter.WriteFieldHeader( 1, WireType.Varint, writer );
            ProtoWriter.WriteUInt32( (uint)date.Day, writer );
            ProtoWriter.WriteFieldHeader( 2, WireType.Varint, writer );
            ProtoWriter.WriteUInt32( (uint)date.Month, writer );
            ProtoWriter.WriteFieldHeader( 3, WireType.Varint, writer );
            ProtoWriter.WriteUInt32( (uint)date.Year, writer );
            ProtoWriter.WriteFieldHeader( 4, WireType.String, writer );
            WriteOffset(writer, date.Offset);
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
            if (value is TimeSpan timeSpan)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Varint, writer);
                ProtoWriter.WriteUInt32((uint)timeSpan.Seconds, writer);
                ProtoWriter.WriteFieldHeader(2, WireType.Varint, writer);
                ProtoWriter.WriteUInt32((uint)timeSpan.Minutes, writer);
                ProtoWriter.WriteFieldHeader(3, WireType.Varint, writer);
                ProtoWriter.WriteUInt32((uint)timeSpan.Hours, writer);
                ProtoWriter.WriteFieldHeader(4, WireType.String, writer);
                var subToken = ProtoWriter.StartSubItem(timeSpan, writer);
                ProtoWriter.EndSubItem(subToken, writer);
                ProtoWriter.WriteFieldHeader(5, WireType.Varint, writer);
                ProtoWriter.WriteUInt32((uint)timeSpan.Milliseconds, writer);
            }
            else if (value is DateTimeOffset dateTimeOffset)
            {

                ProtoWriter.WriteFieldHeader(1, WireType.Varint, writer);
                ProtoWriter.WriteUInt32((uint)dateTimeOffset.Second, writer);
                ProtoWriter.WriteFieldHeader(2, WireType.Varint, writer);
                ProtoWriter.WriteUInt32((uint)dateTimeOffset.Minute, writer);
                ProtoWriter.WriteFieldHeader(3, WireType.Varint, writer);
                ProtoWriter.WriteUInt32((uint)dateTimeOffset.Hour, writer);
                ProtoWriter.WriteFieldHeader(4, WireType.String, writer);
                WriteOffset(writer, dateTimeOffset.Offset);
                ProtoWriter.WriteFieldHeader(5, WireType.Varint, writer);
                ProtoWriter.WriteUInt32((uint)dateTimeOffset.Millisecond, writer);
            }
            else
            {
                throw new NotSupportedException("Expected TimeSpan or DateTimeOffset value for a time");
            }
        }

        private static void WriteOffset(ProtoWriter writer, TimeSpan offset)
        {
            var subToken = ProtoWriter.StartSubItem(offset, writer);
            var hour = offset.Hours;
            var minute = offset.Minutes;
            if (minute < 0)
            {
                minute += 60;
                hour -= 1;
            }
            if (hour != 0)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Varint, writer);
                ProtoWriter.WriteInt32(hour, writer);
            }
            if (minute != 0)
            {
                ProtoWriter.WriteFieldHeader(2, WireType.Varint, writer);
                ProtoWriter.WriteUInt32((uint)minute, writer);
            }
            ProtoWriter.EndSubItem(subToken, writer);
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
                        Write( writer, prop.Value, i + 1, store, element.DataType );
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
                    Write( writer, element, fieldNumber, store, type.DataType );
                }
            }
        }

        private void Write( ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, ConstrainedType type )
        {
            Write( writer, value, fieldNumber, store, type.DataType );
        }

        private void Write( ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, string typeName )
        {
            var type = DataTypeResolver?.Invoke( typeName );
            if(fieldNumber == 0)
            {
                Write( writer, value, 1, store, type );
            }
            else
            {
                ProtoWriter.WriteFieldHeader( fieldNumber, WireType.String, writer );
                var token = ProtoWriter.StartSubItem( value, writer );
                Write( writer, value, 1, store, type );
                ProtoWriter.EndSubItem( token, writer );
            }
        }
    }
}
