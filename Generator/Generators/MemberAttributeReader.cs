using System;
using System.Reflection;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.Generators
{
    internal class MemberAttributeReader : IAttributeReader
    {
        private readonly MemberInfo _member;

        public MemberAttributeReader( MemberInfo member )
        {
            _member = member;
        }

        public T GetCustomAttribute<T>() where T : Attribute
        {
            return _member.GetCustomAttribute<T>();
        }
    }
}
