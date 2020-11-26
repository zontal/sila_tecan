using System;
using System.Reflection;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.Generators
{
    internal class ParameterAttributeReader : IAttributeReader
    {
        private readonly ParameterInfo _parameter;

        public ParameterAttributeReader( ParameterInfo parameter )
        {
            _parameter = parameter;
        }

        public T GetCustomAttribute<T>() where T : Attribute
        {
            return _parameter.GetCustomAttribute<T>();
        }
    }
}
