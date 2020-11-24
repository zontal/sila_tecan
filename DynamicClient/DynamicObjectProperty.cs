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
        public DynamicObjectProperty(SiLAElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

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
        public DynamicObjectProperty(string identifier, string displayName, string description, DataTypeType type)
        {
            Identifier = identifier;
            DisplayName = displayName;
            Description = description;
            Type = type;
        }

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
            set => SetValue(ref _value, value);
        }

        /// <summary>
        /// Sets the given field
        /// </summary>
        /// <typeparam name="T">The type of the field</typeparam>
        /// <param name="field">The field</param>
        /// <param name="value">The value to set</param>
        /// <param name="callerName">The name of the property whose value is set</param>
        protected void SetValue<T>(ref T field, T value, [CallerMemberName] string callerName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(callerName));
            }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Only for internal use
        /// </summary>
        /// <param name="obj"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(object obj)
        {
            Value = obj;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{DisplayName} = {DynamicObject.Print(Value)}";
        }

        /// <inheritdoc />
        public string GetValidationErrors()
        {
            return null;
        }
    }
}
