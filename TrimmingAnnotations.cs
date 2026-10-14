using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Messages and justifications for members with respect to trimming and Native AOT
    /// </summary>
    internal static class TrimmingMessages
    {
        /// <summary>
        /// The message for members that serialize data transfer objects through the reflection-based protobuf-net runtime model
        /// </summary>
        public const string RuntimeTypeModel = "Serializes data transfer objects through the reflection-based protobuf-net runtime model, which is not compatible with trimming and Native AOT. "
            + "Pass a ByteSerializer created from a compile-time generated protobuf-net model instead (see ByteSerializer.Create).";

        /// <summary>
        /// The justification for serializing data transfer objects through a given protobuf-net type model without annotating the data transfer object type
        /// </summary>
        public const string GeneratedTypeModel = "Compile-time generated protobuf-net type models access the members of the data transfer objects statically. "
            + "Reflection-based type models are created through the protobuf-net runtime model, which reports its own trimming warnings.";
    }
}

#if !NET
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false )]
    internal sealed class RequiresUnreferencedCodeAttribute : Attribute
    {
        public RequiresUnreferencedCodeAttribute( string message )
        {
            Message = message;
        }

        public string Message { get; }

        public string Url { get; set; }
    }

    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false )]
    internal sealed class RequiresDynamicCodeAttribute : Attribute
    {
        public RequiresDynamicCodeAttribute( string message )
        {
            Message = message;
        }

        public string Message { get; }

        public string Url { get; set; }
    }

    [AttributeUsage( AttributeTargets.All, Inherited = false, AllowMultiple = true )]
    internal sealed class UnconditionalSuppressMessageAttribute : Attribute
    {
        public UnconditionalSuppressMessageAttribute( string category, string checkId )
        {
            Category = category;
            CheckId = checkId;
        }

        public string Category { get; }

        public string CheckId { get; }

        public string Scope { get; set; }

        public string Target { get; set; }

        public string MessageId { get; set; }

        public string Justification { get; set; }
    }
}
#endif
