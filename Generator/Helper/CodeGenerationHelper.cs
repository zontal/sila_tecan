using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CSharp;

namespace Tecan.Sila2.Generator.Helper
{
    internal static class CodeGenerationHelper
    {
        private static readonly CSharpCodeProvider CSharp = new CSharpCodeProvider();

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

        public static string ToCamelCase(string name)
        {
            return Char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        public static string ToPascalCase(string identifier)
        {
            return Char.ToUpperInvariant(identifier[0]) + identifier.Substring(1);
        }

        public static CodeTypeReference ExtractType(DataTypeType dataType, Action<Constraints> constraintHandler)
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
                    }
                    break;
                case ListType list:
                    return new CodeTypeReference(typeof(ICollection<>).FullName, ExtractType(list.DataType, constraintHandler));
                case ConstrainedType constrained:
                    constraintHandler?.Invoke(constrained.Constraints);
                    return ExtractType(constrained.DataType, constraintHandler);
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


        public static CodeExpression GenerateConversion( CodeMethodInvokeExpression extractionCall, CodeTypeReference customType, DataTypeType dataType )
        {
            if(customType == null)
            {
                return extractionCall;
            }

            if(dataType.Item is BasicType.Binary)
            {
                if(customType.ArrayRank == 1)
                {
                    extractionCall.Method.MethodName = nameof( BinaryDto.ExtractToBytes );
                }
                else if(customType.BaseType == typeof( FileInfo ).FullName)
                {
                    extractionCall.Method.MethodName = nameof( BinaryDto.ExtractToFileInfo );
                }

                return extractionCall;
            }

            return new CodeCastExpression( customType, extractionCall );
        }

        public static void AddAttribute(this CodeAttributeDeclarationCollection attributes, Type attributeType, params object[] values)
        {
            var declaration = new CodeAttributeDeclaration(new CodeTypeReference(attributeType),
                values.Select(o => o as CodeAttributeArgument 
                                   ?? new CodeAttributeArgument(o as CodeExpression 
                                                                ?? new CodePrimitiveExpression(o))).ToArray());

            attributes.Add(declaration);
        }

        public static CodeTypeMember WriteDocumentation( this CodeTypeMember member, string summary, string returns = null, Dictionary<string, string> parameters = null )
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
