using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Denotes a helper class to convert objects to dynamic properties at runtime
    /// </summary>
    public static class RuntimeObjectConverter
    {
        private static readonly Dictionary<Type, ITranslationInformation> _knownTranslations = new Dictionary<Type, ITranslationInformation>();

        static RuntimeObjectConverter()
        {
            var binary = new BasicTypeTranslationInformation( BasicType.Binary );
            var boolean = new BasicTypeTranslationInformation( BasicType.Boolean );
            var integer = new BasicTypeTranslationInformation( BasicType.Integer );
            var real = new BasicTypeTranslationInformation( BasicType.Real );
            var stringType = new BasicTypeTranslationInformation( BasicType.String );
            var timestamp = new BasicTypeTranslationInformation( BasicType.Timestamp );
            var time = new BasicTypeTranslationInformation( BasicType.Time );

            _knownTranslations.Add( typeof( byte[] ), binary );
            _knownTranslations.Add( typeof( Stream ), binary );
            _knownTranslations.Add( typeof( bool ), boolean );
            _knownTranslations.Add( typeof( bool? ), boolean );
            _knownTranslations.Add( typeof( int ), integer );
            _knownTranslations.Add( typeof( int? ), integer );
            _knownTranslations.Add( typeof( long ), integer );
            _knownTranslations.Add( typeof( long? ), integer );
            _knownTranslations.Add( typeof( float ), real );
            _knownTranslations.Add( typeof( float? ), real );
            _knownTranslations.Add( typeof( double ), real );
            _knownTranslations.Add( typeof( double? ), real );
            _knownTranslations.Add( typeof( string ), stringType );
            _knownTranslations.Add( typeof( DateTime ), timestamp );
            _knownTranslations.Add( typeof( DateTimeOffset ), timestamp );
            _knownTranslations.Add( typeof( TimeSpan ), time );
        }

        /// <summary>
        /// Converts the given runtime object into a dynamic object property at runtime
        /// </summary>
        /// <param name="obj">The runtime object that was provided</param>
        /// <param name="identifier">The identifier of the dynamic object property</param>
        /// <param name="description">The description of the dynamic object property</param>
        /// <returns>A dynamic object property if the conversion was successful, or null, otherwise</returns>
        public static DynamicObjectProperty Convert(object obj, string identifier = null, string description = null)
        {
            if (obj == null)
            {
                return null;
            }
            ITranslationInformation translationInfo = null;
            lock(_knownTranslations)
            {
                translationInfo = GetOrCreateTranslation( obj.GetType() );
            }
            if (translationInfo == null)
            {
                return null;
            }
            return new DynamicObjectProperty( identifier, identifier, description, translationInfo.DataType )
            {
                Value = translationInfo.ConvertObject( obj )
            };
        }

        private static ITranslationInformation GetOrCreateTranslation( Type type )
        {
            if(_knownTranslations.TryGetValue( type, out var translationInfo ))
            {
                return translationInfo;
            }

            if(typeof( IEnumerable ).IsAssignableFrom( type ))
            {
                var genericEnumerable = Array.Find( type.GetInterfaces(), t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof( IEnumerable<> ) );
                if (genericEnumerable == null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    genericEnumerable = type;
                }

                if (genericEnumerable != null)
                {
                    var elementTranslation = GetOrCreateTranslation( genericEnumerable.GetGenericArguments()[0] );
                    if (elementTranslation != null)
                    {
                        var translation = new CollectionTypeTranslationInformation( elementTranslation );
                        _knownTranslations.Add( type, translation );
                        return translation;
                    }
                }
                return null;
            }

            if (type.IsEnum)
            {
                var translation = new EnumTranslationInfo( type );
                _knownTranslations.Add( type, translation );
                return translation;
            }

            _knownTranslations.Add( type, null );

            var properties = from property in type.GetProperties()
                             let propertyType = GetOrCreateTranslation( property.PropertyType )
                             where propertyType != null
                             select new PropertyTranslationInfo( property, propertyType );

            var structureTranslation = new StructureTypeTranslationInfo( properties );
#pragma warning disable S4143 // Collection elements should not be replaced unconditionally
            _knownTranslations[type] = structureTranslation;
#pragma warning restore S4143 // Collection elements should not be replaced unconditionally
            return structureTranslation;
        }

        private interface ITranslationInformation
        {
            DataTypeType DataType { get; }

            object ConvertObject( object obj );
        }

        private sealed class BasicTypeTranslationInformation : ITranslationInformation
        {
            public BasicTypeTranslationInformation(BasicType basicType)
            {
                DataType = new DataTypeType()
                {
                    Item = basicType
                };
            }

            public DataTypeType DataType { get; }

            public object ConvertObject( object obj )
            {
                return obj;
            }
        }

        private sealed class CollectionTypeTranslationInformation : ITranslationInformation
        {
            private readonly ITranslationInformation _itemTranslation;

            public CollectionTypeTranslationInformation(ITranslationInformation itemTranslation)
            {
                DataType = new DataTypeType
                {
                    Item = new ListType
                    {
                        DataType = itemTranslation.DataType
                    }
                };
                _itemTranslation = itemTranslation;
            }

            public DataTypeType DataType { get; }

            public object ConvertObject( object obj )
            {
                var list = new List<object>();
                if (obj is IEnumerable enumerable)
                {
                    foreach(var item in enumerable)
                    {
                        list.Add( _itemTranslation.ConvertObject( item ) );
                    }
                }
                return list;
            }
        }

        private sealed class EnumTranslationInfo : ITranslationInformation
        {
            public EnumTranslationInfo(Type enumType)
            {
                DataType = new DataTypeType
                {
                    Item = new ConstrainedType
                    {
                        DataType = new DataTypeType
                        {
                            Item = BasicType.String
                        },
                        Constraints = new Constraints
                        {
                            Set = Enum.GetNames( enumType )
                        }
                    }
                };
            }

            public DataTypeType DataType { get; }

            public object ConvertObject( object obj )
            {
                return obj.ToString();
            }
        }

        private sealed class StructureTypeTranslationInfo : ITranslationInformation
        {
            private readonly List<PropertyTranslationInfo> _properties;

            public StructureTypeTranslationInfo(IEnumerable<PropertyTranslationInfo> properties)
            {
                _properties = properties.ToList();
                DataType = new DataTypeType
                {
                    Item = new StructureType
                    {
                        Element = _properties.Select( p => p.SilaProperty ).ToArray()
                    }
                };
            }

            public DataTypeType DataType { get; }

            public object ConvertObject( object obj )
            {
                if (obj == null)
                {
                    return null;
                }
                var result = new DynamicObject();
                foreach(var property in _properties)
                {
                    var converted = property.ConvertProperty( obj );
                    if(converted.Value != null)
                    {
                        result.Elements.Add( converted );
                    }
                }
                return result;
            }
        }

        private sealed class PropertyTranslationInfo
        {
            private readonly PropertyInfo _property;
            private readonly SiLAElement _silaProperty;
            private readonly ITranslationInformation _translationInfo;

            public PropertyTranslationInfo(PropertyInfo property, ITranslationInformation translationInformation)
            {
                _property = property;
                _translationInfo = translationInformation;
                _silaProperty = new SiLAElement()
                {
                    Identifier = property.Name,
                    DisplayName = property.Name,
                    DataType = translationInformation.DataType
                };

                if (property.GetCustomAttribute<DescriptionAttribute>() is var descriptionAttribute && descriptionAttribute != null)
                {
                    _silaProperty.Description = descriptionAttribute.Description;
                }
            }

            public SiLAElement SilaProperty => _silaProperty;

            public DynamicObjectProperty ConvertProperty(object obj)
            {
                var value = _property.GetValue( obj );
                var converted = _translationInfo.ConvertObject( value );
                return new DynamicObjectProperty( _silaProperty ) { Value = converted };
            }
        }
    }
}
