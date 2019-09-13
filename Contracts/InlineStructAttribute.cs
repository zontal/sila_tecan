using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Defines that the return value of a method can be inlined
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue)]
    public class InlineStructAttribute : Attribute
    {
    }
}
