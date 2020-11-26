using System;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Denotes an object that can read custom attributes
    /// </summary>
    public interface IAttributeReader
    {
        /// <summary>
        /// Gets the custom attribute of the given type or null, if no such attribute can be found
        /// </summary>
        /// <typeparam name="T">The attribute type</typeparam>
        /// <returns>The attribute instance or null</returns>
        T GetCustomAttribute<T>() where T : Attribute;
    }
}
