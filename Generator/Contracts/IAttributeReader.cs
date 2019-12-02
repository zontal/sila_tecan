using System;

namespace Tecan.Sila2.Generator.Contracts
{
    public interface IAttributeReader
    {
        T GetCustomAttribute<T>() where T : Attribute;
    }
}
