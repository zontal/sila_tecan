using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Generator.Contracts
{
    public interface ITypeTranslationInfo
    {
        CodeTypeReference InterfaceType
        {
            get;
        }

        CodeTypeReference DataTransferType
        {
            get;
        }

        CodeExpression Encapsulate( CodeExpression expression, CodeExpression binaryStorageArgument );

        CodeExpression Extract( CodeExpression expression, CodeTypeReference targetType, CodeExpression binaryStorageArgument );
    }
}
