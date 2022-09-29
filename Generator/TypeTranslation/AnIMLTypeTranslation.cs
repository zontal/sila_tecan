using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.TypeTranslation
{
    [Export( typeof( ITypeTranslator ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class AnIMLTypeTranslation : ITypeTranslator, ITypeTranslationInfo
    {
        private const string AnIMLDocumentType = "Tecan.AnIML.AniMlDocument";
        private const string AnIMLDtoType = "Tecan.Sila2.AniMlDto";

        public int Priority => 2;

        public CodeTypeReference InterfaceType => new CodeTypeReference( AnIMLDocumentType );

        public CodeTypeReference DataTransferType => new CodeTypeReference( AnIMLDtoType );

        public CodeExpression Encapsulate( CodeExpression expression, CodeExpression binaryStorageArgument )
        {
            return new CodeObjectCreateExpression( DataTransferType, expression, binaryStorageArgument );
        }

        public CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument )
        {
            return new CodeMethodInvokeExpression( expression, nameof( ISilaTransferObject<string>.Extract ), binaryStorageArgument );
        }

        public CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument, string parameterName )
        {
            return new CodeMethodInvokeExpression( expression, nameof( DtoExtensions.TryExtract ), binaryStorageArgument, new CodePrimitiveExpression( parameterName ) );
        }

        public bool TraverseTypes( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, Action<Type> typeAction )
        {
            return interfaceType.FullName == AnIMLDocumentType;
        }

        public bool TryTranslate( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, out DataTypeType silaType )
        {
            if(interfaceType.FullName == AnIMLDocumentType)
            {
                silaType = new DataTypeType
                {
                    Item = new ConstrainedType
                    {
                        DataType = new DataTypeType
                        {
                            Item = BasicType.Binary
                        },
                        Constraints = new Constraints
                        {
                            ContentType = new ConstraintsContentType
                            {
                                Type = "application",
                                Subtype = "x-animl"
                            }
                        }
                    }
                };
                return true;
            }
            silaType = null;
            return false;
        }

        public bool TryTranslate( ITypeTranslationProvider translationProvider, DataTypeType silaType, string suggestedName, out ITypeTranslationInfo translationInfo, Action<Constraints> constraintHandler = null, Action<string, StructureType> structHandler = null )
        {
            if(silaType.Item is ConstrainedType constrained
                && constrained.Constraints?.ContentType?.Type == "application"
                && constrained.Constraints?.ContentType?.Subtype == "x-animl")
            {
                translationInfo = this;
                return true;
            }
            translationInfo = null;
            return false;
        }
    }
}
