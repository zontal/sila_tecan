using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;

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
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="property"/> is null.</exception>
        public void Deserialize(DynamicObjectProperty property, byte[] data, bool readFieldHeader)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            using (var ms = new MemoryStream(data))
            {
                using (var reader = ProtoReader.Create(ms, null))
                {
                    if (readFieldHeader) reader.ReadFieldHeader();
                    Read(reader, new Action<object>(property.SetValue), (dynamic)property.Type.Item);
                }
            }
        }

        private void Read(ProtoReader reader, Action<object> setProperty, BasicType type)
        {
            var token = ProtoReader.StartSubItem(reader);
            switch (type)
            {
                case BasicType.String:
                    SkipIfNotField1(reader);
                    setProperty(reader.ReadString());
                    break;
                case BasicType.Integer:
                    SkipIfNotField1(reader);
                    setProperty(reader.ReadInt64());
                    break;
                case BasicType.Real:
                    SkipIfNotField1(reader);
                    setProperty(reader.ReadDouble());
                    break; ;
                case BasicType.Boolean:
                    SkipIfNotField1(reader);
                    setProperty(reader.ReadBoolean());
                    break;
                case BasicType.Time:
                    ReadTime(reader, setProperty);
                    break;
                case BasicType.Binary:
                    SkipIfNotField1(reader);
                    setProperty(Encoding.UTF8.GetBytes(reader.ReadString()));
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

        private static void ReadTimestamp(ProtoReader reader, Action<object> setProperty)
        {
            SkipIfNotField1(reader);
            var seconds = reader.ReadUInt32();
            SkipIfNotField1(reader, 2);
            var minute = reader.ReadUInt32();
            SkipIfNotField1(reader, 3);
            var hour = reader.ReadUInt32();
            SkipIfNotField1(reader, 4);
            var day = reader.ReadUInt32();
            SkipIfNotField1(reader, 5);
            var month = reader.ReadUInt32();
            SkipIfNotField1(reader, 6);
            var year = reader.ReadUInt32();
            SkipIfNotField1(reader, 7);
            TimeSpan timeZone = ReadTimezone(reader);
            setProperty(new DateTimeOffset((int)year, (int)month, (int)day, (int)hour, (int)minute, (int)seconds, timeZone));
        }

        private static void ReadDate(ProtoReader reader, Action<object> setProperty)
        {
            SkipIfNotField1(reader);
            var day = reader.ReadUInt32();
            SkipIfNotField1(reader, 2);
            var month = reader.ReadUInt32();
            SkipIfNotField1(reader, 3);
            var year = reader.ReadUInt32();
            SkipIfNotField1(reader, 4);
            TimeSpan timeZone = ReadTimezone(reader);
            setProperty(new DateTimeOffset((int)year, (int)month, (int)day, 0, 0, 0, timeZone));
        }

        private static void ReadTime(ProtoReader reader, Action<object> setProperty)
        {
            SkipIfNotField1(reader);
            var seconds = reader.ReadUInt32();
            SkipIfNotField1(reader, 2);
            var minute = reader.ReadUInt32();
            SkipIfNotField1(reader, 3);
            var hour = reader.ReadUInt32();
            var time = new TimeSpan((int)hour, (int)minute, (int)seconds);
            SkipIfNotField1(reader, 4);
            TimeSpan timeZone = ReadTimezone(reader);
            setProperty(time + timeZone);
        }

        private static TimeSpan ReadTimezone(ProtoReader reader)
        {
            var innerToken = ProtoReader.StartSubItem(reader);
            SkipIfNotField1(reader);
            var timeZoneHour = reader.ReadInt32();
            SkipIfNotField1(reader, 2);
            var timeZoneMinute = reader.ReadUInt32();
            ProtoReader.EndSubItem(innerToken, reader);
            var timeZone = new TimeSpan(timeZoneHour, (int)timeZoneMinute, 0);
            return timeZone;
        }

        private static void SkipIfNotField1(ProtoReader reader, int field = 1)
        {
            if (reader.ReadFieldHeader() != field)
            {
                reader.SkipField();
            }
        }

        private void Read(ProtoReader reader, Action<object> setProperty, ListType type)
        {
            var field = reader.FieldNumber;
            var list = new ObservableCollection<object>();
            var action = new Action<object>(list.Add);
            Read(reader, action,(dynamic)type.DataType.Item);
            while (reader.TryReadFieldHeader(field))
            {
                Read(reader, action, (dynamic)type.DataType.Item);
            }

            setProperty(list);
        }

        private void Read(ProtoReader reader, Action<object> setProperty, StructureType type)
        {
            int field;
            var dynamicObject = new DynamicObject();
            setProperty(dynamicObject);
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
                    Read(reader, new Action<object>(child.SetValue), (dynamic) (fieldType.Item));
                }
                else
                {
                    reader.SkipField();
                }
            }

            if (!first) ProtoReader.EndSubItem(token, reader);
        }

        private void Read(ProtoReader reader, Action<object> setProperty, ConstrainedType type)
        {
            Read(reader, setProperty, (dynamic)(type.DataType.Item));
        }

        private void Read(ProtoReader reader, Action<object> setProperty, string typeName)
        {
            var type = DataTypeResolver?.Invoke(typeName)?.Item;
            if (!(type is StructureType))
            {
                type = new StructureType()
                {
                    Element = new SiLAElement[]
                    {
                        new SiLAElement
                        {
                            DataType = new DataTypeType() {Item = type},
                            Identifier = "Value"
                        }
                    }
                };
            }
            Read(reader, setProperty, (dynamic)(type));
        }

        /// <summary>
        /// Serializes the given dynamic property into a byte array
        /// </summary>
        /// <param name="property">The dynamic property</param>
        /// <returns>A byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="property"/> is null.</exception>
        public byte[] Serialize(DynamicObjectProperty property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            using (var ms = new MemoryStream())
            {
                using (var writer = ProtoWriter.Create(ms, null))
                {
                    Write(writer, property.Value, 0, (dynamic)property.Type.Item);
                    writer.Close();
                    return ms.ToArray();
                }
            }
        }

        private void Write(ProtoWriter writer, object value, int fieldNumber, BasicType type)
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
                    ProtoWriter.WriteFieldHeader(1, WireType.String, writer);
                    ProtoWriter.WriteString(Encoding.UTF8.GetString((byte[])value), writer);
                    break;
                case BasicType.Date:
                    WriteDate(writer, value);
                    break;
                case BasicType.Timestamp:
                    WriteTimestamp(writer, value);
                    break;
            }
            ProtoWriter.EndSubItem(token, writer);
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
            ProtoWriter.WriteInt32(0, writer);
            ProtoWriter.WriteFieldHeader(2, WireType.Fixed32, writer);
            ProtoWriter.WriteUInt32(0, writer);
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

        private void Write(ProtoWriter writer, object value, int fieldNumber, StructureType type)
        {
            var dynamicObject = value as DynamicObject;
            if (dynamicObject == null) return;
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
                        Write(writer, prop.Value, i + 1, (dynamic) element.DataType.Item);
                    }
                }
            }

            if (fieldNumber > 0)
            {
                ProtoWriter.EndSubItem(token, writer);
            }
        }

        private void Write(ProtoWriter writer, object value, int fieldNumber, ListType type)
        {
            if (value is IEnumerable collection)
            {
                foreach (var element in collection)
                {
                    Write(writer, element, fieldNumber, (dynamic)type.DataType.Item);
                }
            }
        }

        private void Write(ProtoWriter writer, object value, int fieldNumber, ConstrainedType type)
        {
            Write(writer, value, fieldNumber, (dynamic)type.DataType.Item);
        }

        private void Write(ProtoWriter writer, object value, int fieldNumber, string typeName)
        {
            var type = DataTypeResolver?.Invoke(typeName)?.Item;
            if (!(type is StructureType))
            {
                type = new StructureType()
                {
                    Element = new SiLAElement[]
                    {
                        new SiLAElement
                        {
                            DataType = new DataTypeType() {Item = type},
                            Identifier = "Value"
                        }
                    }
                };
            }
            Write(writer, value, fieldNumber, (dynamic)type);
        }
    }
}
