using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Specifies that the annotated method or property may throw an exception when executed
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public class ThrowsAttribute : Attribute
    {
        /// <summary>
        /// The type of exception that can be thrown
        /// </summary>
        public Type ExceptionType { get; }

        /// <summary>
        /// Creates a new annotation with the given exception type
        /// </summary>
        /// <param name="exceptionType">The exception type that can be thrown</param>
        public ThrowsAttribute(Type exceptionType)
        {
            ExceptionType = exceptionType;
        }
    }
}
