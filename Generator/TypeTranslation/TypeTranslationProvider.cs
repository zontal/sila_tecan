using Common.Logging;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.TypeTranslation
{
    [Export( typeof( ITypeTranslationProvider ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class TypeTranslationProvider : ITypeTranslationProvider
    {
        private readonly IReadOnlyList<ITypeTranslator> _translators;
        private readonly ILog _loggingChannel = LogManager.GetLogger<TypeTranslationProvider>();

        [ImportingConstructor]
        public TypeTranslationProvider( [ImportMany] IEnumerable<ITypeTranslator> translators )
        {
            _translators = translators.OrderByDescending( t => t.Priority ).ToList();
        }

        public CodeExpression EncapsulateAsDto( CodeExpression argument, DataTypeType dataType, CodeExpression binaryStoreArgument, string suggestedName )
        {
            if(TryTranslate( dataType, suggestedName, out var typeInfo ))
            {
                return typeInfo.Encapsulate( argument, binaryStoreArgument );
            }

            return new CodeObjectCreateExpression( GetDtoTypeReference( dataType, suggestedName, null ), argument, binaryStoreArgument );
        }

        public CodeTypeReference ExtractType( DataTypeType dataType, string suggestedName, Action<Constraints> constraintHandler = null, Action<string, StructureType> registerStructure = null )
        {
            if(TryTranslate( dataType, suggestedName, out var typeInfo, constraintHandler, registerStructure ))
            {
                return typeInfo.InterfaceType;
            }
            throw new NotSupportedException( $"The data type {dataType.Item} is not supported" );
        }

        public CodeTypeReference GetDtoTypeReference( DataTypeType type, string suggestedName, Action<string, StructureType> structHandler )
        {
            if(TryTranslate( type, suggestedName, out var typeInfo, structHandler: structHandler ))
            {
                return typeInfo.DataTransferType;
            }
            throw new NotSupportedException();
        }

        public bool TryTranslate( Type interfaceType, string origin, out DataTypeType silaType )
        {
            foreach(var typeTranslator in _translators)
            {
                if(typeTranslator.TryTranslate( this, interfaceType, origin, out silaType ))
                {
                    _loggingChannel.Debug( $"Type {interfaceType.FullName} translated using {typeTranslator}" );
                    return true;
                }
            }

            silaType = null;
            return false;
        }

        public bool TryTranslate( DataTypeType silaType, string suggestedName, out ITypeTranslationInfo translationInfo, Action<Constraints> constraintHandler = null, Action<string, StructureType> structHandler = null )
        {
            foreach(var typeTranslator in _translators)
            {
                if(typeTranslator.TryTranslate( this, silaType, suggestedName, out translationInfo, constraintHandler, structHandler ) && translationInfo != null)
                {
                    _loggingChannel.Debug( $"SiLA2 Type {silaType.Item} translated using {typeTranslator}" );
                    return true;
                }
            }

            translationInfo = null;
            return false;
        }

        public void TraverseTypes( Type interfaceType, string origin, Action<Type> typeAction )
        {
            foreach(var typeTranslator in _translators)
            {
                if(typeTranslator.TraverseTypes( this, interfaceType, origin, typeAction ))
                {
                    return;
                }
            }
        }

        public CodeExpression Extract(CodeExpression expression, DataTypeType dataType, CodeExpression binaryStorageArgument, CodeTypeReference targetType)
        {
            if (TryTranslate(dataType, null, out var translationInfo))
            {
                return translationInfo.Extract(expression, targetType, binaryStorageArgument);
            }
            throw new NotSupportedException();
        }

        public CodeExpression Extract( CodeExpression expression, DataTypeType dataType, CodeExpression binaryStorageArgument, string parameterName, CodeTypeReference targetType )
        {
            if(TryTranslate( dataType, null, out var translationInfo ))
            {
                return translationInfo.Extract( expression, targetType, binaryStorageArgument, parameterName );
            }
            throw new NotSupportedException();
        }
    }
}
