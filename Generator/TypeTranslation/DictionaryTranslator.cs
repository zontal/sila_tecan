using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

namespace Tecan.Sila2.Generator.TypeTranslation
{
    [Export( typeof( ITypeTranslator ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class DictionaryTranslator : ITypeTranslator
    {
        private readonly ICodeNameRegistry _nameRegistry;

        [ImportingConstructor]
        public DictionaryTranslator( ICodeNameRegistry nameRegistry )
        {
            _nameRegistry = nameRegistry;
        }

        public bool TryTranslate( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, out DataTypeType silaType )
        {
            if(IsDictionaryType( interfaceType, out var keyType, out var valueType )
                && translationProvider.TryTranslate( keyType, origin + ".Key", out var keySilaType )
                && translationProvider.TryTranslate( valueType, origin + ".Value", out var valueSilaType ))
            {

                if(interfaceType.GetGenericTypeDefinition() != typeof( IDictionary<,> ))
                {
                    _nameRegistry?.RegisterDifferentType( origin, interfaceType );
                }

                silaType = new DataTypeType()
                {
                    Item = new ListType()
                    {
                        DataType = new DataTypeType()
                        {
                            Item = new StructureType()
                            {
                                Element = new[]
                                {
                                    new SiLAElement()
                                    {
                                        Identifier = "Key",
                                        DisplayName = "Key",
                                        Description = "",
                                        DataType = keySilaType
                                    },
                                    new SiLAElement()
                                    {
                                        Identifier = "Value",
                                        DisplayName = "Value",
                                        Description = "",
                                        DataType = valueSilaType
                                    }
                                }
                            }
                        }
                    }
                };
                return true;
            }

            silaType = null;
            return false;
        }

        public bool TryTranslate( ITypeTranslationProvider translationProvider, DataTypeType silaType, string suggestedName, out ITypeTranslationInfo translationInfo,
            Action<Constraints> constraintHandler = null, Action<string, StructureType> structHandler = null )
        {
            if(silaType.Item is ListType list && list.DataType.Item is StructureType structure && IsKeyValueStructure( structure ))
            {
                var keyType = structure.Element[0].DataType;
                var valueType = structure.Element[1].DataType;
                var itemName = CodeGenerationHelper.TurnSingular( suggestedName );
                if(translationProvider.TryTranslate( keyType, itemName + "Key", out var keyTranslation, null, structHandler )
                    && translationProvider.TryTranslate( valueType, itemName + "Value", out var valueTranslation, constraintHandler, structHandler ))
                {
                    translationInfo = new TranslationInfo( keyTranslation, valueTranslation );
                    return true;
                }
            }

            translationInfo = null;
            return false;
        }

        private bool IsDictionaryType( Type type, out Type keyType, out Type elementType )
        {
            keyType = null;
            elementType = null;
            if(type.IsGenericType && typeof( IEnumerable ).IsAssignableFrom( type ))
            {
                var typeDefinition = type.GetGenericTypeDefinition();
                if(typeDefinition == typeof( IReadOnlyDictionary<,> )
                   || typeDefinition == typeof( IDictionary<,> ))
                {
                    keyType = type.GetGenericArguments()[0];
                    elementType = type.GetGenericArguments()[1];
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Gets a value indicating whether the given data type is a key value structure
        /// </summary>
        /// <param name="structure">The SiLA2 data type</param>
        /// <returns>True, if <paramref name="structure"/> is a key value pair</returns>
        public static bool IsKeyValueStructure( StructureType structure )
        {
            return structure.Element != null && structure.Element.Length == 2 && structure.Element[0].Identifier == "Key"
                   && structure.Element[1].Identifier == "Value";
        }

        public bool TraverseTypes( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, Action<Type> typeAction )
        {
            if(IsDictionaryType( interfaceType, out var keyType, out var elementType ))
            {
                var itemName = CodeGenerationHelper.TurnSingular( origin );
                translationProvider.TraverseTypes( keyType, itemName + "Key", typeAction );
                translationProvider.TraverseTypes( elementType, itemName + "Value", typeAction );
                return true;
            }

            return false;
        }

        public int Priority => 2;

        private class TranslationInfo : ITypeTranslationInfo
        {
            private readonly ITypeTranslationInfo _keyTranslation;
            private readonly ITypeTranslationInfo _valueTranslation;

            public TranslationInfo( ITypeTranslationInfo keyTranslation, ITypeTranslationInfo valueTranslation )
            {
                _keyTranslation = keyTranslation;
                _valueTranslation = valueTranslation;
            }

            public CodeTypeReference InterfaceType => new CodeTypeReference( typeof( IDictionary<,> ).FullName, _keyTranslation.InterfaceType, _valueTranslation.InterfaceType );

            public CodeTypeReference DataTransferType
            {
                get
                {
                    if(_valueTranslation.InterfaceType.TypeArguments.Count == 1 && _valueTranslation.DataTransferType.TypeArguments.Count == 1)
                    {
                        return new CodeTypeReference( typeof( List<> ).FullName, new CodeTypeReference( typeof( LookupEntryDto<,,,> ).FullName,
                            _keyTranslation.InterfaceType, _keyTranslation.DataTransferType, _valueTranslation.InterfaceType.TypeArguments[0], _valueTranslation.DataTransferType.TypeArguments[0] ) );
                    }
                    else
                    {
                        return new CodeTypeReference( typeof( List<> ).FullName, new CodeTypeReference( typeof( KeyValuePairDto<,,,> ).FullName,
                            _keyTranslation.InterfaceType, _keyTranslation.DataTransferType, _valueTranslation.InterfaceType, _valueTranslation.DataTransferType ) );
                    }
                }
            }

            public CodeExpression Encapsulate( CodeExpression expression, CodeExpression binaryStorageArgument )
            {
                var valueType = _valueTranslation.DataTransferType;
                if(valueType.TypeArguments.Count == 1)
                {
                    valueType = valueType.TypeArguments[0];
                }
                return new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression( typeof( DtoExtensions ) ),
                    nameof( DtoExtensions.Encapsulate ),
                    expression,
                    new CodeMethodReferenceExpression( new CodeTypeReferenceExpression( _keyTranslation.DataTransferType ), "Create" ),
                    new CodeMethodReferenceExpression( new CodeTypeReferenceExpression( valueType ), "Create" ),
                    binaryStorageArgument );
            }

            public CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument )
            {
                return new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression( typeof( DtoExtensions ) ),
                    nameof( DtoExtensions.Extract ),
                    expression, binaryStorageArgument );
            }

            public CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument, string parameterName )
            {
                return new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression( typeof( DtoExtensions ) ),
                    nameof( DtoExtensions.TryExtract ),
                    expression, binaryStorageArgument,
                    new CodePrimitiveExpression( parameterName ) );
            }
        }
    }
}
