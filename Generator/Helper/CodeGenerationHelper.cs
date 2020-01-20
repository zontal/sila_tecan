using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using Tecan.Sila2.DynamicClient;

namespace Tecan.Sila2.Generator.Helper
{
    /// <summary>
    /// A helper class for the code generation
    /// </summary>
    public static class CodeGenerationHelper
    {
        private static readonly CSharpCodeProvider CSharp = new CSharpCodeProvider();

        /// <summary>
        /// Generates the C# code of the given compilation unit to the given file path
        /// </summary>
        /// <param name="unit">The CodeDOM unit that contains the generated code</param>
        /// <param name="path">The file path where the code should be generated to</param>
        public static void GenerateCSharp(CodeCompileUnit unit, string path)
        {
            var options = new CodeGeneratorOptions
            {
                BlankLinesBetweenMembers = true,
                BracingStyle = "C",
                ElseOnClosing = true,
                IndentString = "    ",
                VerbatimOrder = false
            };
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (directory != null && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
            using (var writer = new StreamWriter(Path.GetFullPath(path)))
            {
                CSharp.GenerateCodeFromCompileUnit(unit, writer, options);
            }
        }

        /// <summary>
        /// Converts the given identifier to camel case
        /// </summary>
        /// <param name="identifier">An identifier (i.e. without whitespaces)</param>
        /// <returns>A camel case representation of the identifier</returns>
        public static string ToCamelCase( this string identifier )
        {
            return Char.ToLowerInvariant( identifier[0]) + identifier.Substring(1);
        }

        /// <summary>
        /// Converts the given identifier to Pascal case
        /// </summary>
        /// <param name="identifier">An identifier (i.e. without whitespaces)</param>
        /// <returns>A Pascal case representation of the identifier</returns>
        public static string ToPascalCase(this string identifier)
        {
            return Char.ToUpperInvariant(identifier[0]) + identifier.Substring(1);
        }

        /// <summary>
        /// Extracts the default type reference for the given SiLA2 type
        /// </summary>
        /// <param name="dataType">The SiLA2 type</param>
        /// <param name="suggestedName">The suggested name of an anonymous SiLA2 type</param>
        /// <param name="constraintHandler">A callback if constraints are detected or null</param>
        /// <param name="registerStructure">A callback if anonymous structures are identified</param>
        /// <returns>The CodeDOM type reference of the SiLA2 type definition</returns>
        public static CodeTypeReference ExtractType( DataTypeType dataType, string suggestedName, Action<Constraints> constraintHandler = null,
            Action<string, StructureType> registerStructure = null )
        {
            switch (dataType.Item)
            {
                case BasicType basic:
                    switch (basic)
                    {
                        case BasicType.Boolean:
                            return new CodeTypeReference(typeof(bool));
                        case BasicType.Date:
                        case BasicType.Timestamp:
                            return new CodeTypeReference(typeof(DateTimeOffset));
                        case BasicType.Time:
                            return new CodeTypeReference(typeof(TimeSpan));
                        case BasicType.Integer:
                            return new CodeTypeReference(typeof(long));
                        case BasicType.Binary:
                            return new CodeTypeReference(typeof(Stream));
                        case BasicType.String:
                            return new CodeTypeReference(typeof(string));
                        case BasicType.Real:
                            return new CodeTypeReference(typeof(double));
                        case BasicType.Any:
                            return new CodeTypeReference(typeof(DynamicObjectProperty));
                    }
                    break;
                case ListType list:
                    if( list.DataType.Item is StructureType inlineStructure && inlineStructure.Element != null && inlineStructure.Element.Length == 2
                        && inlineStructure.Element[0].Identifier == "Key" && inlineStructure.Element[1].Identifier == "Value" )
                    {
                        return new CodeTypeReference(typeof(IDictionary<,>).FullName, 
                            ExtractType(inlineStructure.Element[0].DataType, TurnSingular(suggestedName) + "Key", null, registerStructure ),
                            ExtractType(inlineStructure.Element[1].DataType, TurnSingular(suggestedName) + "Value", constraintHandler, registerStructure ));
                    }
                    return new CodeTypeReference(typeof(ICollection<>).FullName, ExtractType(list.DataType, TurnSingular(suggestedName), constraintHandler, registerStructure ) );
                case ConstrainedType constrained:
                    constraintHandler?.Invoke(constrained.Constraints);
                    return ExtractType(constrained.DataType, suggestedName, constraintHandler, registerStructure );
                case StructureType structured:
                    registerStructure?.Invoke( suggestedName, structured );
                    return new CodeTypeReference(suggestedName);
                case string identityReference:
                    var isAllUpperCase = identityReference.All( char.IsUpper );
                    if( isAllUpperCase )
                    {
                        return new CodeTypeReference(identityReference[0] + identityReference.Substring(1).ToLowerInvariant());
                    }
                    return new CodeTypeReference(identityReference);
            }
            throw new NotSupportedException($"The data type {dataType.Item} is not supported");
        }

        /// <summary>
        /// Turns the given identifier into a singular representation
        /// </summary>
        /// <param name="name">The identifier</param>
        /// <returns>A singular representation</returns>
        public static string TurnSingular( string name )
        {
            if( name.EndsWith( "s" ) )
            {
                return name.Substring( 0, name.Length - 1 );
            }
            else
            {
                return name + "Item";
            }
        }

        /// <summary>
        /// Wraps the given extraction call into a conversion
        /// </summary>
        /// <param name="extractedResult">A call declaration of the Extract method</param>
        /// <param name="dataType">The data type that should be extracted</param>
        /// <param name="customType">The custom type</param>
        /// <returns>A code expression that extracts the DTO into the correct type</returns>
        public static CodeExpression GenerateConversion( CodeMethodInvokeExpression extractedResult, DataTypeType dataType, CodeTypeReference customType )
        {
            if(customType == null)
            {
                return extractedResult;
            }

            if(dataType.Item is BasicType.Binary)
            {
                if(customType.ArrayRank == 1)
                {
                    extractedResult.Method.MethodName = nameof( BinaryDto.ExtractToBytes );
                }
                else if(customType.BaseType == typeof( FileInfo ).FullName)
                {
                    extractedResult.Method.MethodName = nameof( BinaryDto.ExtractToFileInfo );
                }

                return extractedResult;
            }

            return new CodeCastExpression( customType, extractedResult );
        }

        /// <summary>
        /// Adds an attribute to the given collection of custom attributes
        /// </summary>
        /// <param name="attributes">The attributes collection</param>
        /// <param name="attributeType">The type of the attribute to add</param>
        /// <param name="values">Constructor parameters for the attribute either as code expressions or primitive values</param>
        public static void AddAttribute(this CodeAttributeDeclarationCollection attributes, Type attributeType, params object[] values)
        {
            var declaration = new CodeAttributeDeclaration(new CodeTypeReference(attributeType),
                values.Select(o => o as CodeAttributeArgument 
                                   ?? new CodeAttributeArgument(o as CodeExpression 
                                                                ?? new CodePrimitiveExpression(o))).ToArray());

            attributes.Add(declaration);
        }

        /// <summary>
        /// Generates documentation for the given code member
        /// </summary>
        /// <param name="member">The member for which code should be generated</param>
        /// <param name="summary">The summary that should be generated</param>
        /// <param name="returns">The returns value that should be generated</param>
        /// <param name="parameters">A dictionary with parameter definitions</param>
        /// <returns>The member (for chaining purposes)</returns>
        public static CodeTypeMember WriteDocumentation( this CodeTypeMember member, string summary, string returns = null, IReadOnlyDictionary<string, string> parameters = null )
        {
            var documentationWriter = new StringBuilder();
            documentationWriter.AppendLine( " <summary>" );
            if( summary != null )
            {
                foreach( var line in summary.Split( '\n' ) )
                {
                    var trimmed = line.Trim();
                    if( !string.IsNullOrEmpty( trimmed ) )
                    {
                        documentationWriter.AppendLine( " " + trimmed );
                    }
                }
            }

            documentationWriter.Append( " </summary>" );
            if( parameters != null )
            {
                foreach( var parameter in parameters )
                {
                    documentationWriter.AppendLine();
                    documentationWriter.Append( $" <param name=\"{parameter.Key}\">{parameter.Value}</param>" );
                }
            }

            if( returns != null )
            {
                documentationWriter.AppendLine();
                documentationWriter.Append( $" <returns>{returns}</returns>" );
            }

            member.Comments.Add( new CodeCommentStatement( documentationWriter.ToString(), true ) );
            return member;
        }
    }
}
