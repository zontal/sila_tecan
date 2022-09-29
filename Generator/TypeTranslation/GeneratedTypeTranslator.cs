using System;
using System.CodeDom;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.TypeTranslation
{
    [Export( typeof( ITypeTranslator ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class GeneratedTypeTranslator : ITypeTranslator
    {
        public bool TryTranslate( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, out DataTypeType silaType )
        {
            if((interfaceType.IsClass || interfaceType.IsValueType || interfaceType.IsEnum) && !interfaceType.IsGenericTypeDefinition)
            {
                var identifierAttribute = interfaceType.GetCustomAttribute<SilaIdentifierAttribute>();
                silaType = new DataTypeType()
                {
                    Item = identifierAttribute?.Identifier ?? interfaceType.Name
                };
                return true;
            }

            silaType = null;
            return false;
        }

        public bool TraverseTypes( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, Action<Type> typeAction )
        {
            if((interfaceType.IsClass || interfaceType.IsValueType || interfaceType.IsEnum) && !interfaceType.IsGenericTypeDefinition)
            {
                typeAction?.Invoke( interfaceType );
                return true;
            }

            return false;
        }

        public bool TryTranslate( ITypeTranslationProvider translationProvider, DataTypeType silaType, string suggestedName, out ITypeTranslationInfo translationInfo,
            Action<Constraints> constraintHandler = null, Action<string, StructureType> structHandler = null )
        {
            if(silaType.Item is string dataTypeIdentifier)
            {
                var isAllUpperCase = dataTypeIdentifier.All( char.IsUpper );
                if(isAllUpperCase)
                {
                    dataTypeIdentifier = dataTypeIdentifier[0] + dataTypeIdentifier.Substring( 1 ).ToLowerInvariant();
                }
                translationInfo = new TranslationInfo( dataTypeIdentifier );
                return true;
            }

            if(silaType.Item is StructureType structure)
            {
                structHandler?.Invoke( suggestedName, structure );
                translationInfo = new TranslationInfo( suggestedName );
                return true;
            }

            translationInfo = null;
            return false;
        }

        public int Priority => 0;

        private class TranslationInfo : ITypeTranslationInfo
        {
            private readonly string _name;

            public TranslationInfo( string name )
            {
                _name = name;
            }

            public CodeTypeReference InterfaceType => new CodeTypeReference( _name );

            public CodeTypeReference DataTransferType => new CodeTypeReference( _name + "Dto" );

            public CodeExpression Encapsulate( CodeExpression expression, CodeExpression binaryStorageArgument )
            {
                return new CodeObjectCreateExpression( DataTransferType, expression, binaryStorageArgument );
            }

            public CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument )
            {
                return new CodeMethodInvokeExpression( expression, nameof( ISilaTransferObject<object>.Extract ), binaryStorageArgument );
            }

            public CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument, string parameterName )
            {
                return new CodeMethodInvokeExpression( expression, nameof( DtoExtensions.TryExtract ), binaryStorageArgument, new CodePrimitiveExpression( parameterName ) );
            }
        }
    }
}
