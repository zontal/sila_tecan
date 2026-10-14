using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.Helper
{
    /// <summary>
    /// Helper class for the compile-time protobuf-net type model and the byte serializers generated for a feature
    /// </summary>
    public static class SerializationModelHelper
    {
        /// <summary>
        /// The id of the diagnostic that protobuf-net reports for its experimental compile-time model API
        /// </summary>
        public const string ExperimentalModelDiagnosticId = "PBN9001";

        /// <summary>
        /// Gets a value indicating whether the generated code should serialize through a compile-time generated protobuf-net type model
        /// </summary>
        public static bool UseGeneratedModel => !GeneratorSettings.UseRuntimeTypeModel;

        /// <summary>
        /// Gets the name of the protobuf-net type model generated for the given feature
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <returns>The name of the type model class</returns>
        public static string GetModelName( Feature feature )
        {
            return feature.Identifier + "TypeModel";
        }

        /// <summary>
        /// Gets the name of the class that holds the byte serializers generated for the given feature
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <returns>The name of the serializers class</returns>
        public static string GetSerializersName( Feature feature )
        {
            return feature.Identifier + "Serializers";
        }

        /// <summary>
        /// Gets the name of the field that holds the byte serializer for the given data transfer type
        /// </summary>
        /// <param name="dtoType">The data transfer type</param>
        /// <returns>A valid C# identifier</returns>
        public static string GetSerializerFieldName( CodeTypeReference dtoType )
        {
            var baseType = dtoType.BaseType;
            var lastDot = baseType.LastIndexOf( '.' );
            if(lastDot != -1)
            {
                baseType = baseType.Substring( lastDot + 1 );
            }
            var arity = baseType.IndexOf( '`' );
            if(arity != -1)
            {
                baseType = baseType.Substring( 0, arity );
            }
            if(dtoType.TypeArguments.Count == 0)
            {
                return baseType;
            }
            return baseType + "Of" + string.Join( "And", dtoType.TypeArguments.Cast<CodeTypeReference>().Select( GetSerializerFieldName ) );
        }

        /// <summary>
        /// Determines whether the given data transfer type is the empty request, which has a dedicated serializer
        /// </summary>
        /// <param name="dtoType">The data transfer type</param>
        /// <returns>True, if the type denotes the empty request</returns>
        public static bool IsEmptyRequest( CodeTypeReference dtoType )
        {
            return dtoType.TypeArguments.Count == 0 && (dtoType.BaseType == nameof( EmptyRequest ) || dtoType.BaseType == typeof( EmptyRequest ).FullName);
        }

        /// <summary>
        /// Creates an expression that references the byte serializer for the given data transfer type
        /// </summary>
        /// <param name="feature">The feature for which code is generated</param>
        /// <param name="dtoType">The data transfer type</param>
        /// <returns>A code expression of type ByteSerializer&lt;T&gt;</returns>
        public static CodeExpression GetSerializer( Feature feature, CodeTypeReference dtoType )
        {
            if(IsEmptyRequest( dtoType ))
            {
                return new CodeFieldReferenceExpression( new CodeTypeReferenceExpression( nameof( EmptyRequest ) ), nameof( EmptyRequest.Marshaller ) );
            }
            return new CodeFieldReferenceExpression( new CodeTypeReferenceExpression( GetSerializersName( feature ) ), GetSerializerFieldName( dtoType ) );
        }

        /// <summary>
        /// Gets the data transfer types that the generated code of a feature serializes directly
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="nameProvider">The name provider used to name the data transfer types</param>
        /// <param name="translationProvider">The type translation provider</param>
        /// <returns>A collection of distinct type references</returns>
        public static IReadOnlyList<CodeTypeReference> GetRootTypes( Feature feature, ICodeNameProvider nameProvider, ITypeTranslationProvider translationProvider )
        {
            var roots = new List<CodeTypeReference>();
            var names = new HashSet<string>( StringComparer.Ordinal );

            void Add( CodeTypeReference type )
            {
                if(type != null && !IsEmptyRequest( type ) && names.Add( GetSerializerFieldName( type ) ))
                {
                    roots.Add( type );
                }
            }

            if(feature.Items == null)
            {
                return roots;
            }

            foreach(var command in feature.Items.OfType<FeatureCommand>())
            {
                Add( nameProvider.GenerateCommandRequestType( command ) );
                if(command.IntermediateResponse != null && command.IntermediateResponse.Length > 0)
                {
                    Add( nameProvider.GenerateCommandIntermediateType( command ) );
                }
                if(command.Response != null && command.Response.Length > 0)
                {
                    Add( nameProvider.GenerateCommandResponseType( command ) );
                }
            }

            foreach(var property in feature.Items.OfType<FeatureProperty>())
            {
                Add( nameProvider.GetPropertyResponseType( property, translationProvider ) );
            }

            foreach(var metadata in feature.Items.OfType<FeatureMetadata>())
            {
                Add( GetMetadataType( metadata, translationProvider ) );
            }

            return roots;
        }

        /// <summary>
        /// Gets the type of the message that transports the given metadata
        /// </summary>
        /// <param name="metadata">The metadata</param>
        /// <param name="translationProvider">The type translation provider</param>
        /// <param name="structHandler">A handler for anonymous structures or null</param>
        /// <returns>A reference to the closed PropertyResponse&lt;T&gt; type of the metadata</returns>
        public static CodeTypeReference GetMetadataType( FeatureMetadata metadata, ITypeTranslationProvider translationProvider, Action<string, StructureType> structHandler = null )
        {
            return new CodeTypeReference( typeof( PropertyResponse<> ).Name, translationProvider.GetDtoTypeReference( metadata.DataType, metadata.Identifier, structHandler ) );
        }
    }
}
