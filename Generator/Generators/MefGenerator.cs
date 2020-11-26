using System.CodeDom;
using System.ComponentModel.Composition;
using System.Linq;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.Generators
{
    internal class MefGenerator : IDependencyInjectionGenerator
    {
        public void AddDependencyInjectionRegistrations( CodeTypeDeclaration codeTypeDeclaration )
        {
            foreach( CodeTypeReference baseType in codeTypeDeclaration.BaseTypes )
            {
                codeTypeDeclaration.CustomAttributes.Add( new CodeAttributeDeclaration( new CodeTypeReference( typeof( ExportAttribute ) ),
                    new CodeAttributeArgument( new CodeTypeOfExpression( baseType ) ) ) );

            }
            codeTypeDeclaration.CustomAttributes.Add( new CodeAttributeDeclaration( typeof( PartCreationPolicyAttribute ).FullName,
                new CodeAttributeArgument( new CodeFieldReferenceExpression(
                    new CodeTypeReferenceExpression( typeof( CreationPolicy ) ), nameof( CreationPolicy.Shared ) ) ) ) );

            var constructor = codeTypeDeclaration.Members.OfType<CodeConstructor>().SingleOrDefault();
            constructor?.CustomAttributes.Add( new CodeAttributeDeclaration( new CodeTypeReference( typeof(ImportingConstructorAttribute) ) ) );
        }
    }
}
