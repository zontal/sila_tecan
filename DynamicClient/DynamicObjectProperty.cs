using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tecan.Sila2.DynamicClient
{
    public class DynamicObjectProperty : INotifyPropertyChanged
    {
        public DynamicObjectProperty(SiLAElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            Identifier = element.Identifier;
            DisplayName = element.DisplayName;
            Description = element.Description;
            Type = element.DataType;
        }

        public DynamicObjectProperty(string identifier, string displayName, string description, DataTypeType type)
        {
            Identifier = identifier;
            DisplayName = displayName;
            Description = description;
            Type = type;
        }

        public string Identifier { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public DataTypeType Type { get; }

        private object _value;

        public object Value
        {
            get => _value;
            set => SetValue(ref _value, value);
        }

        protected void SetValue<T>(ref T field, T value, [CallerMemberName] string callerName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(callerName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(object obj)
        {
            Value = obj;
        }

        public override string ToString()
        {
            return $"{DisplayName} = {DynamicObject.Print(Value)}";
        }
    }
}
