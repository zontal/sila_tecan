using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using Tecan.Sila2.DynamicClient;
using Tecan.Sila2.Generator.Contracts;

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
        /// Converts the given identifier into a display name
        /// </summary>
        /// <param name="identifier">The identifier</param>
        /// <returns>A display name where a blank space is inserted before each upper-case letter</returns>
        public static string ToDisplayName( this string identifier )
        {
            if( String.IsNullOrEmpty( identifier ) )
            {
                return String.Empty;
            }

            if( identifier.All( Char.IsUpper ) )
            {
                return identifier;
            }

            var sb = new StringBuilder();
            sb.Append( identifier[0] );
            for( int i = 1; i < identifier.Length; i++ )
            {
                if( Char.IsUpper( identifier[i] ) )
                {
                    sb.Append( ' ' );
                }

                sb.Append( identifier[i] );
            }

            return sb.ToString();
        }

        /// <summary>
        /// Determines whether the given command is a setter command
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="possibleProperties">A collection of possible properties</param>
        /// <returns>True if the command is a setter</returns>
        public static bool IsSetterCommand( FeatureCommand command, params FeatureProperty[] possibleProperties )
        {
            return IsSetterCommand( command, possibleProperties as IEnumerable<FeatureProperty> );
        }

        /// <summary>
        /// Determines whether the given command is a setter command
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="possibleProperties">A collection of possible properties</param>
        /// <returns>True if the command is a setter</returns>
        public static bool IsSetterCommand( FeatureCommand command, IEnumerable<FeatureProperty> possibleProperties )
        {
            if(command.Identifier == null || command.Identifier.Length <= 3 || !command.Identifier.StartsWith( "Set" ) ||
               command.Parameter == null || command.Parameter.Length != 1 || possibleProperties == null)
            {
                return false;
            }
            foreach(var featureProperty in possibleProperties)
            {
                if(command.Identifier == "Set" + featureProperty.Identifier && IsSameType( featureProperty.DataType, command.Parameter[0].DataType ))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type1"></param>
        /// <param name="type2"></param>
        /// <returns></returns>
        public static bool IsSameType( DataTypeType type1, DataTypeType type2 )
        {
            return Equals( type1?.Item, type2?.Item );
        }

        /// <summary>
        /// Turns the given identifier into a singular representation
        /// </summary>
        /// <param name="name">The identifier</param>
        /// <returns>A singular representation</returns>
        public static string TurnSingular( string name )
        {
            if( name == null )
            {
                return null;
            }
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
                    if( !String.IsNullOrEmpty( trimmed ) )
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
