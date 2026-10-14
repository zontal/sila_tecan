using System;
using System.Diagnostics.CodeAnalysis;

namespace Tecan.Sila2
{
    /// <summary>
    /// Messages for members that are not compatible with trimming or Native AOT
    /// </summary>
    internal static class TrimmingMessages
    {
        /// <summary>
        /// The message for members that serialize data transfer objects through the reflection-based protobuf-net runtime model
        /// </summary>
        public const string RuntimeTypeModel = "Serializes data transfer objects through the reflection-based protobuf-net runtime model, which is not compatible with trimming and Native AOT. "
            + "Pass a ByteSerializer created from a compile-time generated protobuf-net model instead (see ByteSerializer.Create).";
    }

    /// <summary>
    /// Annotations for members that are compatible with trimming and Native AOT
    /// </summary>
    internal static class TrimmingAnnotations
    {
        /// <summary>
        /// The members of a data transfer object type that protobuf-net type models may access, as declared by protobuf-net
        /// </summary>
        public const DynamicallyAccessedMemberTypes DataTransferObjectMembers =
            DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods
            | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields
            | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes
            | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties;
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

    [Flags]
    internal enum DynamicallyAccessedMemberTypes
    {
        None = 0,
        PublicParameterlessConstructor = 0x0001,
        PublicConstructors = 0x0002 | PublicParameterlessConstructor,
        NonPublicConstructors = 0x0004,
        PublicMethods = 0x0008,
        NonPublicMethods = 0x0010,
        PublicFields = 0x0020,
        NonPublicFields = 0x0040,
        PublicNestedTypes = 0x0080,
        NonPublicNestedTypes = 0x0100,
        PublicProperties = 0x0200,
        NonPublicProperties = 0x0400,
        PublicEvents = 0x0800,
        NonPublicEvents = 0x1000,
        Interfaces = 0x2000,
        All = ~None
    }

    [AttributeUsage( AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = false )]
    internal sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        public DynamicallyAccessedMembersAttribute( DynamicallyAccessedMemberTypes memberTypes )
        {
            MemberTypes = memberTypes;
        }

        public DynamicallyAccessedMemberTypes MemberTypes { get; }
    }
}
#endif
