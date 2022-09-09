using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Represents a property of a dynamic object
    /// </summary>
    public class DynamicObjectProperty : INotifyPropertyChanged, ISilaTransferObject
    {
        /// <summary>
        /// Creates a dynamic property
        /// </summary>
        /// <param name="element">The SiLA element to encapsulate in a dynamic property</param>
        public DynamicObjectProperty( SiLAElement element )
        {
            if(element == null) throw new ArgumentNullException( nameof( element ) );

            Identifier = element.Identifier;
            DisplayName = element.DisplayName;
            Description = element.Description;
            Type = element.DataType;
        }

        /// <summary>
        /// Creates a dynamic property
        /// </summary>
        /// <param name="identifier">The identifier of the property</param>
        /// <param name="displayName">The display name of the property</param>
        /// <param name="description">The description of the property</param>
        /// <param name="type">The property type</param>
        public DynamicObjectProperty( string identifier, string displayName, string description, DataTypeType type )
        {
            Identifier = identifier;
            DisplayName = displayName;
            Description = description;
            Type = type;
        }

        /// <summary>
        /// Creates a dynamic object property for the given value
        /// </summary>
        /// <param name="value">The value that is to be encapsulated</param>
        /// <param name="dataType">The SiLA2 data type of the value</param>
        public DynamicObjectProperty( object value, DataTypeType dataType )
        {
            Value = value;
            Type = dataType;
        }

        /// <summary>
        /// Creates a dynamic object property for the given value
        /// </summary>
        /// <param name="value">The value that is to be encapsulated</param>
        /// <param name="basicType">The basic SiLA2 data type of the value</param>
        public DynamicObjectProperty( object value, BasicType basicType ) : this( value, new DataTypeType { Item = basicType } )
        {
        }

        /// <summary>
        /// Creates a dynamic object property for the given value
        /// </summary>
        /// <param name="stringValue">The string value</param>
        public DynamicObjectProperty( string stringValue ) : this( stringValue, BasicType.String ) { }

        /// <summary>
        /// Creates a dynamic object property for the given value
        /// </summary>
        /// <param name="doubleValue">The real value</param>
        public DynamicObjectProperty( double doubleValue ) : this( doubleValue, BasicType.Real ) { }

        /// <summary>
        /// Creates a dynamic object property for the given value
        /// </summary>
        /// <param name="longValue">The integer value</param>
        public DynamicObjectProperty( long longValue ) : this( longValue, BasicType.Integer ) { }

        /// <summary>
        /// Creates a dynamic object property for the given value
        /// </summary>
        /// <param name="booleanValue">The boolean value</param>
        public DynamicObjectProperty( bool booleanValue ) : this( booleanValue, BasicType.Boolean ) { }

        /// <summary>
        /// Creates a dynamic object property for the given value
        /// </summary>
        /// <param name="binaryValue">The binary value</param>
        public DynamicObjectProperty( byte[] binaryValue ) : this( binaryValue, BasicType.Binary ) { }

        /// <summary>
        /// Creates a dynamic object property for the given value
        /// </summary>
        /// <param name="timeStampValue">The timestamp value</param>
        public DynamicObjectProperty( DateTimeOffset timeStampValue ) : this( timeStampValue, BasicType.Timestamp ) { }

        /// <summary>
        /// Creates a dynamic object property for the given value
        /// </summary>
        /// <param name="dynamicObject">The dynamic object</param>
        public DynamicObjectProperty( DynamicObject dynamicObject ) : this( dynamicObject, dynamicObject?.CreateType() ?? throw new ArgumentNullException( nameof( dynamicObject ) ) ) { }

        /// <summary>
        /// Gets the identifier of the property
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Gets the display name of the property
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the description of the property
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the property type
        /// </summary>
        public DataTypeType Type { get; }

        private object _value;

        /// <summary>
        /// Gets or sets the property value
        /// </summary>
        public object Value
        {
            get => _value;
            set => SetValue( ref _value, value );
        }

        /// <summary>
        /// Sets the given field
        /// </summary>
        /// <typeparam name="T">The type of the field</typeparam>
        /// <param name="field">The field</param>
        /// <param name="value">The value to set</param>
        /// <param name="callerName">The name of the property whose value is set</param>
        protected void SetValue<T>( ref T field, T value, [CallerMemberName] string callerName = null )
        {
            if(!EqualityComparer<T>.Default.Equals( field, value ))
            {
                field = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( callerName ) );
            }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Only for internal use
        /// </summary>
        /// <param name="obj"></param>
        [EditorBrowsable( EditorBrowsableState.Never )]
        internal void SetValue( object obj )
        {
            Value = obj;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{DisplayName} = {DynamicObject.Print( Value )}";
        }

        /// <inheritdoc />
        public string GetValidationErrors()
        {
            return null;
        }

        [EditorBrowsable( EditorBrowsableState.Never )]
        internal void MergeValue( object obj )
        {
            if(Value is ICollection<object> collection)
            {
                if(obj is IEnumerable<object> collectionToMerge)
                {
                    foreach(var item in collectionToMerge)
                    {
                        collection.Add( item );
                    }
                }
                else
                {
                    collection.Add( obj );
                }
            }
            else
            {
                Value = obj;
            }
        }
    }
}
