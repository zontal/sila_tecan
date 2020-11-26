using System;
using System.ComponentModel.Composition;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.TypeTranslation
{
    [Export(typeof(ITypeTranslator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ConstraintsTranslator : ITypeTranslator
    {
        public bool TryTranslate( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, out DataTypeType silaType )
        {
            silaType = null;
            return false;
        }

        public bool TraverseTypes( ITypeTranslationProvider translationProvider, Type interfaceType, string origin, Action<Type> typeAction )
        {
            return false;
        }

        public bool TryTranslate( ITypeTranslationProvider translationProvider, DataTypeType silaType, string suggestedName, out ITypeTranslationInfo translationInfo,
            Action<Constraints> constraintHandler = null, Action<string, StructureType> structHandler = null )
        {
            if( silaType.Item is ConstrainedType constrained && translationProvider.TryTranslate(constrained.DataType, suggestedName, out translationInfo, constraintHandler, structHandler) )
            {
                constraintHandler?.Invoke(constrained.Constraints);
                return true;
            }

            translationInfo = null;
            return false;
        }

        public int Priority => 1;
    }
}
