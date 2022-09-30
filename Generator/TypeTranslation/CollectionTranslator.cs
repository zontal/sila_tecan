using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

namespace Tecan.Sila2.Generator.TypeTranslation
{
    [Export( typeof( ITypeTranslator ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class CollectionTranslator : ITypeTranslator
    {
        private readonly ICodeNameRegistry _nameRegistry;

        [ImportingConstructor]
        public CollectionTranslator( ICodeNameRegistry nameRegistry )
        {
            _nameRegistry = nameRegistry;
        }

        public bool TryTranslate( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, out DataTypeType silaType )
        {
            if(IsCollectionType( interfaceType, out var elementType ) && translationProvider.TryTranslate( elementType, origin + ".Item", out var elementSilaType ))
            {
                if(interfaceType.IsArray
                    || interfaceType.GetGenericTypeDefinition() != typeof( ICollection<> )
                    || _nameRegistry.IsDifferentTypeRegistered( origin + ".Item" ))
                {
                    _nameRegistry.RegisterDifferentType( origin, interfaceType );
                }

                silaType = new DataTypeType()
                {
                    Item = new ListType()
                    {
                        DataType = elementSilaType
                    }
                };
                return true;
            }

            silaType = null;
            return false;
        }


        private bool IsCollectionType( Type type, out Type elementType )
        {
            elementType = null;

            if(type.IsArray)
            {
                elementType = type.GetElementType();
                return true;
            }

            if(type.IsGenericType && typeof( IEnumerable ).IsAssignableFrom( type ))
            {
                var typeDefinition = type.GetGenericTypeDefinition();
                if(typeDefinition == typeof( IEnumerable<> )
                   || typeDefinition == typeof( ICollection<> )
                   || typeDefinition == typeof( IReadOnlyList<> )
                   || typeDefinition == typeof( IReadOnlyCollection<> )
                   || typeDefinition == typeof( IList<> )
                   || typeDefinition == typeof( List<> ))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }

            return false;
        }

        public bool TryTranslate( ITypeTranslationProvider translationProvider, DataTypeType silaType, string suggestedName, out ITypeTranslationInfo translationInfo,
            Action<Constraints> constraintHandler = null, Action<string, StructureType> structHandler = null )
        {
            translationInfo = null;
            if(IsCollection( silaType, out var elementType ) && translationProvider.TryTranslate( elementType, CodeGenerationHelper.TurnSingular( suggestedName ), out var elementTranslationInfo, constraintHandler, structHandler ))
            {
                translationInfo = new TranslationInfo( elementTranslationInfo );
                return true;
            }

            return false;
        }


        /// <summary>
        /// Gets a value indicating whether the given data type is a collection
        /// </summary>
        /// <param name="dataType">The SiLA2 data type</param>
        /// <param name="innerType">The element type</param>
        /// <returns>True, if <paramref name="dataType"/> is a collection, otherwise False.</returns>
        public static bool IsCollection( DataTypeType dataType, out DataTypeType innerType )
        {
            switch(dataType.Item)
            {
                case ListType list:
                    innerType = list.DataType;
                    return true;
                case ConstrainedType constrained:
                    return IsCollection( constrained.DataType, out innerType );
                default:
                    innerType = null;
                    return false;
            }
        }

        public bool TraverseTypes( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, Action<Type> typeAction )
        {
            if(IsCollectionType( interfaceType, out var elementType ))
            {
                translationProvider.TraverseTypes( elementType, CodeGenerationHelper.TurnSingular( origin ), typeAction );
                return true;
            }

            return false;
        }

        public int Priority => 1;

        private class TranslationInfo : ITypeTranslationInfo
        {
            private readonly ITypeTranslationInfo _elementTranslationInfo;

            public TranslationInfo( ITypeTranslationInfo elementTranslationInfo )
            {
                _elementTranslationInfo = elementTranslationInfo;
            }

            public CodeTypeReference InterfaceType => new CodeTypeReference( typeof( ICollection<> ).FullName, _elementTranslationInfo.InterfaceType );

            public CodeTypeReference DataTransferType => new CodeTypeReference( typeof( List<> ).FullName, _elementTranslationInfo.DataTransferType );

            public CodeExpression Encapsulate( CodeExpression expression, CodeExpression binaryStorageArgument )
            {
                var innerTypeRef = _elementTranslationInfo.DataTransferType;
                var wrapped = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression( typeof( DtoExtensions ) ),
                    nameof( DtoExtensions.Encapsulate ),
                    expression,
                    new CodeMethodReferenceExpression( new CodeTypeReferenceExpression( innerTypeRef ), "Create" ),
                    binaryStorageArgument );
                return wrapped;
            }

            public CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument )
            {
                if(targetType != null)
                {
                    if(targetType.ArrayRank != 0)
                    {

                        if(targetType.BaseType != _elementTranslationInfo.InterfaceType.BaseType)
                        {
                            return new CodeMethodInvokeExpression(
                                new CodeMethodReferenceExpression(
                                    expression,
                                    nameof( DtoExtensions.ExtractConvertedToArray ),
                                    _elementTranslationInfo.InterfaceType,
                                    new CodeTypeReference( targetType.BaseType )
                                    ),
                                binaryStorageArgument );
                        }

                        return new CodeMethodInvokeExpression(
                            expression,
                            nameof( DtoExtensions.ExtractToArray ),
                            binaryStorageArgument );
                    }
                    if(targetType.TypeArguments.Count == 0)
                    {
                        throw new NotSupportedException( "Custom collection types are not supported" );
                    }

                    if(targetType.TypeArguments[0].BaseType != _elementTranslationInfo.InterfaceType.BaseType)
                    {
                        return new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                expression,
                                nameof( DtoExtensions.ExtractConverted ),
                                _elementTranslationInfo.InterfaceType,
                                targetType.TypeArguments[0]
                                ),
                            binaryStorageArgument );
                    }
                }
                return new CodeMethodInvokeExpression(
                    expression,
                    nameof( DtoExtensions.Extract ),
                    binaryStorageArgument );
            }

            public CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument, string parameterName )
            {
                if(targetType != null)
                {
                    if(targetType.ArrayRank != 0)
                    {

                        if(targetType.BaseType != _elementTranslationInfo.InterfaceType.BaseType)
                        {
                            return new CodeMethodInvokeExpression(
                                new CodeMethodReferenceExpression(
                                    expression,
                                    nameof( DtoExtensions.TryExtractConvertedToArray ),
                                    _elementTranslationInfo.InterfaceType,
                                    new CodeTypeReference( targetType.BaseType )
                                    ),
                                binaryStorageArgument,
                                new CodePrimitiveExpression( parameterName ) );
                        }

                        return new CodeMethodInvokeExpression(
                            expression,
                            nameof( DtoExtensions.TryExtractToArray ),
                            binaryStorageArgument,
                            new CodePrimitiveExpression( parameterName ) );
                    }
                    if(targetType.TypeArguments.Count == 0)
                    {
                        throw new NotSupportedException( "Custom collection types are not supported" );
                    }

                    if(targetType.TypeArguments[0].BaseType != _elementTranslationInfo.InterfaceType.BaseType)
                    {
                        return new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                expression,
                                nameof( DtoExtensions.TryExtractConverted ),
                                _elementTranslationInfo.InterfaceType,
                                targetType.TypeArguments[0]
                                ),
                            binaryStorageArgument,
                            new CodePrimitiveExpression( parameterName ) );
                    }
                }
                return new CodeMethodInvokeExpression(
                    expression,
                    nameof( DtoExtensions.TryExtract ),
                    binaryStorageArgument,
                    new CodePrimitiveExpression( parameterName ) );
            }
        }
    }
}
