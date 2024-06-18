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
    [Export(typeof(ITypeTranslator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class GuidTypeTranslation : ITypeTranslator, ITypeTranslationInfo
    {
        private const string GuidPattern = "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}";
        private const string GuidLength = "36";

        public CodeTypeReference InterfaceType => new CodeTypeReference(typeof(Guid));

        public CodeTypeReference DataTransferType => new CodeTypeReference(typeof(GuidDto));

        public int Priority => 2;

        public CodeExpression Encapsulate(CodeExpression expression, CodeExpression binaryStorageArgument)
        {
            return new CodeObjectCreateExpression(DataTransferType, expression, binaryStorageArgument);
        }

        public CodeExpression Extract(CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument)
        {
            return new CodeMethodInvokeExpression(expression, nameof(ISilaTransferObject<string>.Extract), binaryStorageArgument);
        }

        public CodeExpression Extract(CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument, string parameterName)
        {
            return new CodeMethodInvokeExpression(expression, nameof(DtoExtensions.TryExtract), binaryStorageArgument, new CodePrimitiveExpression(parameterName));
        }

        public bool TraverseTypes(ITypeTranslationProvider translationProvider, Type interfaceType, string origin, Action<Type> typeAction)
        {
            return interfaceType == typeof(Guid);
        }

        public bool TryTranslate(ITypeTranslationProvider translationProvider, Type interfaceType, string origin, out DataTypeType silaType)
        {
            if (interfaceType == typeof(Guid))
            {
                silaType = new DataTypeType
                {
                    Item = new ConstrainedType
                    {
                        Constraints = new Constraints
                        {
                            Pattern = GuidPattern,
                            Length = GuidLength
                        },
                        DataType = new DataTypeType
                        {
                            Item = BasicType.String
                        }
                    }
                };
                return true;
            }
            silaType = null;
            return false;
        }

        public bool TryTranslate(ITypeTranslationProvider translationProvider, DataTypeType silaType, string suggestedName, out ITypeTranslationInfo translationInfo, Action<Constraints> constraintHandler = null, Action<string, StructureType> structHandler = null)
        {
            if (silaType.Item is ConstrainedType constrained
                && constrained.Constraints?.Pattern == GuidPattern
                && constrained.Constraints?.Length == GuidLength)
            {
                translationInfo = this;
                return true;
            }
            translationInfo = null;
            return false;
        }
    }
}
