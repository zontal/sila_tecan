using System;
using System.Collections;
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
        public DynamicObjectSerializer(Func<string, DataTypeType> dataTypeResolver)
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
        public void Deserialize(DynamicObjectProperty property, byte[] data, bool readFieldHeader, IBinaryStore binaryStore )
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            using (var ms = new MemoryStream(data))
            {
                using (var reader = ProtoReader.Create(ms, null))
                {
                    if (readFieldHeader) reader.ReadFieldHeader();
                    Read(reader, new Action<object>(property.SetValue), binaryStore, (dynamic)property.Type.Item);
                }
            }
        }

        private void Read(ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, BasicType type)
        {
            var token = ProtoReader.StartSubItem(reader);
            switch (type)
            {
                case BasicType.Any:
                    ReadAny(reader, setProperty, binaryStore);
                    break;
                case BasicType.String:
                    if( IsInField( reader, 1 ) )
                    {
                        setProperty( reader.ReadString() );
                    }
                    break;
                case BasicType.Integer:
                    if( IsInField( reader, 1 ) )
                    {
                        setProperty( reader.ReadInt64() );
                    }
                    else
                    {
                        setProperty( 0L );
                    }
                    break;
                case BasicType.Real:
                    if( IsInField( reader, 1 ) )
                    {
                        setProperty( reader.ReadDouble() );
                    }
                    else
                    {
                        setProperty( 0.0 );
                    }
                    break; ;
                case BasicType.Boolean:
                    if( IsInField( reader, 1 ) )
                    {
                        setProperty( reader.ReadBoolean() );
                    }
                    else
                    {
                        setProperty( false );
                    }
                    break;
                case BasicType.Time:
                    ReadTime(reader, setProperty);
                    break;
                case BasicType.Binary:
                    if( IsInField( reader, 1 ) )
                    {
                        setProperty( new MemoryStream( Encoding.UTF8.GetBytes( reader.ReadString() ) ) );
                    }
                    else if (IsInField(reader, 2))
                    {
                        setProperty( binaryStore.ResolveStoredBinary( reader.ReadString() ) );
                    }
                    break;
                case BasicType.Date:
                    ReadDate(reader, setProperty);
                    break;
                case BasicType.Timestamp:
                    ReadTimestamp(reader, setProperty);
                    break;
            }
            ProtoReader.EndSubItem(token, reader);
        }

        private static int ReadUIntOfField( ProtoReader reader, int field )
        {
            if(IsInField( reader, field ))
            {
                return (int)reader.ReadUInt32();
            }
            else
            {
                return 0;
            }
        }

        private static void ReadAny( ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore )
        {
            var dto = new AnyTypeDto();
            if( IsInField( reader, 1 ) )
            {
                dto.Type = reader.ReadString();
            }
            if(IsInField( reader, 2 ))
            {
                dto.Payload = Encoding.UTF8.GetBytes( reader.ReadString() );
            }

            setProperty( dto.Extract( binaryStore ) );
        }

        private static void ReadTimestamp(ProtoReader reader, Action<object> setProperty)
        {
            var seconds = ReadUIntOfField( reader, 1 );
            var minute = ReadUIntOfField( reader, 2 );
            var hour = ReadUIntOfField( reader, 3 );
            var day = ReadUIntOfField( reader, 4 );
            var month = ReadUIntOfField( reader, 5 );
            var year = ReadUIntOfField( reader, 6 );
            TimeSpan timeZone = ReadTimezone(reader, 7);
            setProperty(new DateTimeOffset((int)year, (int)month, (int)day, (int)hour, (int)minute, (int)seconds, timeZone));
        }

        private static void ReadDate(ProtoReader reader, Action<object> setProperty)
        {
            var day = ReadUIntOfField( reader, 1 );
            var month = ReadUIntOfField( reader, 2 );
            var year = ReadUIntOfField( reader, 3 );
            TimeSpan timeZone = ReadTimezone(reader, 4);
            setProperty(new DateTimeOffset((int)year, (int)month, (int)day, 0, 0, 0, timeZone));
        }

        private static void ReadTime(ProtoReader reader, Action<object> setProperty)
        {
            var seconds = ReadUIntOfField( reader, 1 );
            var minute = ReadUIntOfField( reader, 2 );
            var hour = ReadUIntOfField( reader, 3 );

            var time = new TimeSpan(hour, minute, seconds);
            TimeSpan timeZone = ReadTimezone( reader, 4 );

            setProperty(time + timeZone);
        }

        private static TimeSpan ReadTimezone(ProtoReader reader, int field)
        {
            if( IsInField( reader, field ) )
            {
                var innerToken = ProtoReader.StartSubItem( reader );
                int timeZoneHour = 0;
                if( IsInField( reader, 1 ) )
                {
                    timeZoneHour = reader.ReadInt32();
                }

                var timeZoneMinute = ReadUIntOfField( reader, 2 );
                ProtoReader.EndSubItem( innerToken, reader );
                var timeZone = new TimeSpan( timeZoneHour, (int)timeZoneMinute, 0 );
                return timeZone;
            }
            else
            {
                return TimeSpan.Zero;
            }
        }

        private static bool IsInField( ProtoReader reader, int field )
        {
            return reader.TryReadFieldHeader( field );
        }

        private void Read(ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, ListType type)
        {
            var field = reader.FieldNumber;
            var list = new ObservableCollection<object>();
            var action = new Action<object>(list.Add);
            Read(reader, action, binaryStore,(dynamic)type.DataType.Item);
            while (reader.TryReadFieldHeader(field))
            {
                Read(reader, action, binaryStore, (dynamic)type.DataType.Item);
            }

            setProperty(list);
        }

        private void Read(ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, StructureType type)
        {
            int field;
            var dynamicObject = new DynamicObject();
            var first = reader.LongPosition == 0;
            SubItemToken token = default(SubItemToken);
            if (!first) token = ProtoReader.StartSubItem(reader);
            while ((field = reader.ReadFieldHeader()) > 0)
            {
                if (field <= type.Element.Length)
                {
                    var element = type.Element[field - 1];
                    var fieldType = element.DataType;
                    var child = new DynamicObjectProperty(element);
                    dynamicObject.Elements.Add(child);
                    Read(reader, new Action<object>(child.SetValue), binaryStore, (dynamic) (fieldType.Item));
                }
                else
                {
                    reader.SkipField();
                }
            }

            if( !first )
            {
                ProtoReader.EndSubItem( token, reader );
            }

            if( first || dynamicObject.Elements.Count > 0)
            {
                setProperty( dynamicObject );
            }
        }

        private void Read(ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, ConstrainedType type)
        {
            Read(reader, setProperty, binaryStore, (dynamic)(type.DataType.Item));
        }

        private void Read(ProtoReader reader, Action<object> setProperty, IBinaryStore binaryStore, string typeName)
        {
            var type = DataTypeResolver?.Invoke(typeName)?.Item;
            var token = ProtoReader.StartSubItem( reader );
            reader.ReadFieldHeader();
            Read(reader, setProperty, binaryStore, (dynamic)(type));
            ProtoReader.EndSubItem( token, reader );
        }

        /// <summary>
        /// Serializes the given dynamic property into a byte array
        /// </summary>
        /// <param name="property">The dynamic property</param>
        /// <param name="store">The binary store to resolve streams</param>
        /// <returns>A byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="property"/> is null.</exception>
        public byte[] Serialize(DynamicObjectProperty property, IBinaryStore store )
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            using (var ms = new MemoryStream())
            {
                using (var writer = ProtoWriter.Create(ms, null))
                {
                    Write(writer, property.Value, 0, store, (dynamic)property.Type.Item);
                    writer.Close();
                    return ms.ToArray();
                }
            }
        }

        private void Write(ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, BasicType type)
        {
            ProtoWriter.WriteFieldHeader(fieldNumber, WireType.String, writer);
            var token = ProtoWriter.StartSubItem(value, writer);
            switch (type)
            {
                case BasicType.String:
                    ProtoWriter.WriteFieldHeader(1, WireType.String, writer);
                    ProtoWriter.WriteString((string)value, writer);
                    break;
                case BasicType.Integer:
                    ProtoWriter.WriteFieldHeader(1, WireType.Fixed64, writer);
                    ProtoWriter.WriteInt64((long)value, writer);
                    break;
                case BasicType.Real:
                    ProtoWriter.WriteFieldHeader(1, WireType.Fixed64, writer);
                    ProtoWriter.WriteDouble((double)value, writer);
                    break;
                case BasicType.Boolean:
                    ProtoWriter.WriteFieldHeader(1, WireType.Fixed32, writer);
                    ProtoWriter.WriteBoolean((bool)value, writer);
                    break;
                case BasicType.Time:
                    WriteTime(writer, value);
                    break;
                case BasicType.Binary:
                    WriteBinary(writer, value, store);
                    break;
                case BasicType.Date:
                    WriteDate(writer, value);
                    break;
                case BasicType.Timestamp:
                    WriteTimestamp(writer, value);
                    break;
                case BasicType.Any:
                    WriteAny( writer, value, store );
                    break;
            }
            ProtoWriter.EndSubItem(token, writer);
        }

        private void WriteAny( ProtoWriter writer, object value, IBinaryStore store )
        {
            if( value is DynamicObjectProperty property )
            {
                ProtoWriter.WriteFieldHeader( 1, WireType.String, writer );
                ProtoWriter.WriteString( DataTypeSerializer.SaveToString( property.Type ), writer );
                ProtoWriter.WriteFieldHeader( 2, WireType.String, writer );
                ProtoWriter.WriteString( Encoding.UTF8.GetString( AnyTypeDto.Serializer.Serialize( property, store ) ), writer );
            }
        }

        private static void WriteBinary(ProtoWriter writer, object value, IBinaryStore store)
        {
            switch( value )
            {
                case Stream stream:
                    if( stream.Length > 0 && store.ShouldStoreBinary( stream.Length ) )
                    {
                        WriteBinaryIdentifier(writer, store.StoreBinary(stream));
                    }
                    else
                    {
                        using( var ms = new MemoryStream() )
                        {
                            stream.CopyTo(ms);
                            WriteBinaryData(writer, ms.ToArray());
                        }
                    }
                    return;
                case byte[] data:
                    if( store.ShouldStoreBinary( data.LongLength ) )
                    {
                        using( var ms = new MemoryStream(data) )
                        {
                            WriteBinaryIdentifier(writer, store.StoreBinary( ms ));
                        }
                    }
                    else
                    {
                        WriteBinaryData(writer, data);
                    }
                    return;
                case FileInfo fileInfo:
                    WriteBinaryIdentifier( writer, store.StoreBinary( fileInfo ) );
                    return;
                default: throw new InvalidOperationException($"Cannot transfer {value} as binary, need Byte array, Stream or FileInfo.");
            }
        }

        private static void WriteBinaryData(ProtoWriter writer, byte[] data)
        {
            ProtoWriter.WriteFieldHeader(1, WireType.String, writer);
            ProtoWriter.WriteString(Encoding.UTF8.GetString(data), writer);
        }

        private static void WriteBinaryIdentifier(ProtoWriter writer, string identifier)
        {
            ProtoWriter.WriteFieldHeader(2, WireType.String, writer);
            ProtoWriter.WriteString(identifier, writer);
        }

        private static void WriteTimestamp(ProtoWriter writer, object value)
        {
            var dateTimeOffset = (DateTimeOffset)value;
            ProtoWriter.WriteFieldHeader(1, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)dateTimeOffset.Second, writer);
            ProtoWriter.WriteFieldHeader(2, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)dateTimeOffset.Minute, writer);
            ProtoWriter.WriteFieldHeader(3, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)dateTimeOffset.Hour, writer);
            ProtoWriter.WriteFieldHeader(4, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)dateTimeOffset.Day, writer);
            ProtoWriter.WriteFieldHeader(5, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)dateTimeOffset.Month, writer);
            ProtoWriter.WriteFieldHeader(6, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)dateTimeOffset.Year, writer);
            ProtoWriter.WriteFieldHeader(7, WireType.String, writer);
            var subToken = ProtoWriter.StartSubItem(dateTimeOffset, writer);
            ProtoWriter.WriteFieldHeader(1, WireType.Fixed32, writer);
            ProtoWriter.WriteInt32(dateTimeOffset.Offset.Hours, writer);
            ProtoWriter.WriteFieldHeader(2, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)dateTimeOffset.Offset.Minutes, writer);
            ProtoWriter.EndSubItem(subToken, writer);
        }

        private static void WriteDate(ProtoWriter writer, object value)
        {
            var date = (DateTimeOffset)value;
            ProtoWriter.WriteFieldHeader(1, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)date.Day, writer);
            ProtoWriter.WriteFieldHeader(2, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)date.Month, writer);
            ProtoWriter.WriteFieldHeader(3, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)date.Year, writer);
            ProtoWriter.WriteFieldHeader(4, WireType.String, writer);
            var subToken = ProtoWriter.StartSubItem(date, writer);
            ProtoWriter.WriteFieldHeader(1, WireType.Fixed32, writer);
            ProtoWriter.WriteInt32(0, writer);
            ProtoWriter.WriteFieldHeader(2, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32(0, writer);
            ProtoWriter.EndSubItem(subToken, writer);
        }

        private static void WriteTime(ProtoWriter writer, object value)
        {
            var timeSpan = (TimeSpan)value;
            ProtoWriter.WriteFieldHeader(1, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)timeSpan.Seconds, writer);
            ProtoWriter.WriteFieldHeader(2, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)timeSpan.Minutes, writer);
            ProtoWriter.WriteFieldHeader(3, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32((uint)timeSpan.Hours, writer);
            ProtoWriter.WriteFieldHeader(4, WireType.String, writer);
            var subToken = ProtoWriter.StartSubItem(timeSpan, writer);
            ProtoWriter.WriteFieldHeader(1, WireType.Fixed32, writer);
            ProtoWriter.WriteInt32(0, writer);
            ProtoWriter.WriteFieldHeader(2, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32(0, writer);
            ProtoWriter.EndSubItem(subToken, writer);
        }

        private void Write(ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, StructureType type)
        {
            var dynamicObject = value as DynamicObject;
            if( dynamicObject == null )
            {
                if( value != null )
                {
                    throw new InvalidOperationException($"Cannot write {value} as structured type. Expecting Dynamic Object instead.");
                }

                return;
            }
            SubItemToken token = default(SubItemToken);
            if (fieldNumber > 0)
            {
                ProtoWriter.WriteFieldHeader(fieldNumber, WireType.String, writer);
                token = ProtoWriter.StartSubItem(dynamicObject, writer);
            }

            if (type.Element != null)
            {
                for (int i = 0; i < type.Element.Length; i++)
                {
                    var element = type.Element[i];
                    var prop = dynamicObject.Elements.FirstOrDefault(p => p.Identifier == element.Identifier);
                    if (prop != null)
                    {
                        Write(writer, prop.Value, i + 1, store, (dynamic) element.DataType.Item);
                    }
                }
            }

            if (fieldNumber > 0)
            {
                ProtoWriter.EndSubItem(token, writer);
            }
        }

        private void Write(ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, ListType type)
        {
            if (value is IEnumerable collection)
            {
                foreach (var element in collection)
                {
                    Write(writer, element, fieldNumber, store, (dynamic)type.DataType.Item);
                }
            }
        }

        private void Write(ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, ConstrainedType type)
        {
            Write(writer, value, fieldNumber, store, (dynamic)type.DataType.Item);
        }

        private void Write(ProtoWriter writer, object value, int fieldNumber, IBinaryStore store, string typeName)
        {
            var type = DataTypeResolver?.Invoke(typeName)?.Item;

            ProtoWriter.WriteFieldHeader( fieldNumber, WireType.String, writer );
            var token = ProtoWriter.StartSubItem( value, writer );
            Write(writer, value, 1, store, (dynamic)type);
            ProtoWriter.EndSubItem( token, writer );
        }
    }
}
