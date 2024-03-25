using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CSharp;

#pragma warning disable S3265 // Non-flags enums should not be used in bitwise operations

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
        public static void GenerateCSharp( CodeCompileUnit unit, string path )
        {
            var options = new CodeGeneratorOptions
            {
                BlankLinesBetweenMembers = true,
                BracingStyle = "C",
                ElseOnClosing = true,
                IndentString = "    ",
                VerbatimOrder = false
            };
            var directory = Path.GetDirectoryName( Path.GetFullPath( path ) );
            if(directory != null && !Directory.Exists( directory )) Directory.CreateDirectory( directory );
            using(var writer = new StreamWriter( Path.GetFullPath( path ) ))
            {
                CSharp.GenerateCodeFromCompileUnit( unit, writer, options );
            }
        }

        /// <summary>
        /// Converts the given identifier to camel case
        /// </summary>
        /// <param name="identifier">An identifier (i.e. without whitespaces)</param>
        /// <returns>A camel case representation of the identifier</returns>
        public static string ToCamelCase( this string identifier )
        {
            return Char.ToLowerInvariant( identifier[0] ) + identifier.Substring( 1 );
        }

        /// <summary>
        /// Converts the given identifier to Pascal case
        /// </summary>
        /// <param name="identifier">An identifier (i.e. without whitespaces)</param>
        /// <returns>A Pascal case representation of the identifier</returns>
        public static string ToPascalCase( this string identifier )
        {
            return Char.ToUpperInvariant( identifier[0] ) + identifier.Substring( 1 );
        }

        /// <summary>
        /// Adds an element to the given array
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <param name="array">The array</param>
        /// <param name="element">The element</param>
        /// <returns>The new array</returns>
        public static T[] Add<T>( this T[] array, T element )
        {
            if(array == null || array.Length == 0)
            {
                return new[] { element };
            }
            var newArray = new T[array.Length + 1];
            Array.Copy( array, newArray, array.Length );
            newArray[array.Length] = element;
            return newArray;
        }

        /// <summary>
        /// Creates a code expression that represents the formatted value with the given format
        /// </summary>
        /// <param name="targetObject">The object that is to be included in the format string</param>
        /// <param name="format">The format string</param>
        /// <returns>A code expression that represents the formatting</returns>
        public static CodeExpression Format( this CodeExpression targetObject, string format )
        {
            return new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( typeof( string ) ),
                nameof( string.Format ),
                new CodePrimitiveExpression( format ),
                targetObject );
        }

        /// <summary>
        /// Converts the given identifier into a display name
        /// </summary>
        /// <param name="identifier">The identifier</param>
        /// <returns>A display name where a blank space is inserted before each upper-case letter</returns>
        public static string ToDisplayName( this string identifier )
        {
            if(String.IsNullOrEmpty( identifier ))
            {
                return String.Empty;
            }

            if(identifier.All( Char.IsUpper ))
            {
                return identifier;
            }

            var sb = new StringBuilder();
            sb.Append( identifier[0] );
            for(int i = 1; i < identifier.Length; i++)
            {
                if(Char.IsUpper( identifier[i] ))
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
            return IsSetterCommand( command, possibleProperties as IEnumerable<FeatureProperty>, out var _ );
        }

        /// <summary>
        /// Determines whether the given command is a setter command
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="possibleProperties">A collection of possible properties</param>
        /// <param name="property">Returns the property</param>
        /// <returns>True if the command is a setter</returns>
        public static bool IsSetterCommand( FeatureCommand command, IEnumerable<FeatureProperty> possibleProperties, out FeatureProperty property )
        {
            property = null;
            if(command.Identifier == null || command.Identifier.Length <= 3 || !command.Identifier.StartsWith( "Set" ) ||
               command.Parameter == null || command.Parameter.Length != 1 || possibleProperties == null)
            {
                return false;
            }
            foreach(var featureProperty in possibleProperties)
            {
                if(command.Identifier == "Set" + featureProperty.Identifier && IsSameType( featureProperty.DataType, command.Parameter[0].DataType ))
                {
                    property = featureProperty;
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
            if(name == null)
            {
                return null;
            }
            if(name.EndsWith( "s" ))
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
        public static void AddAttribute( this CodeAttributeDeclarationCollection attributes, Type attributeType, params object[] values )
        {
            var declaration = new CodeAttributeDeclaration( new CodeTypeReference( attributeType ),
                values.Select( o => o as CodeAttributeArgument
                                   ?? new CodeAttributeArgument( o as CodeExpression
                                                                ?? new CodePrimitiveExpression( o ) ) ).ToArray() );

            attributes.Add( declaration );
        }

        /// <summary>
        /// Adds a field to the given type declaration and adds a parameter in the given constructor
        /// </summary>
        /// <param name="declaringType">The type that declares the field</param>
        /// <param name="name">The name of the field, without a prefix</param>
        /// <param name="type">The type of the field</param>
        /// <param name="constructor">The constructor to which a parameter should be added or null</param>
        /// <returns>A field reference</returns>
        public static CodeFieldReferenceExpression AddField( this CodeTypeDeclaration declaringType, string name, CodeTypeReference type, CodeConstructor constructor )
        {
            var fieldDeclaration = new CodeMemberField
            {
                Name = "_" + name,
                Type = type,
                Attributes = MemberAttributes.Private | MemberAttributes.Final
            };
            var fieldReference = new CodeFieldReferenceExpression( null, fieldDeclaration.Name );
            declaringType.Members.Add( fieldDeclaration );
            constructor?.Parameters.Add( new CodeParameterDeclarationExpression( type, name ) );
            constructor?.Statements.Add( new CodeAssignStatement( fieldReference, new CodeArgumentReferenceExpression( name ) ) );
            return fieldReference;
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
            var written = false;
            var documentationWriter = new StringBuilder();
            if(!string.IsNullOrEmpty( summary ))
            {
                Write( "summary", null, summary, documentationWriter, true, ref written );
            }

            if(parameters != null)
            {
                foreach(var parameter in parameters)
                {
                    Write( "param", parameter.Key, parameter.Value, documentationWriter, false, ref written );
                }
            }

            Write( "returns", null, returns, documentationWriter, false, ref written );

            if(documentationWriter.Length > 0)
            {
                member.Comments.Add( new CodeCommentStatement( documentationWriter.ToString(), true ) );
            }
            return member;
        }

        private static void Write( string tag, string name, string value, StringBuilder documentationWriter, bool forceElement, ref bool written )
        {
            if(string.IsNullOrEmpty( value ))
            {
                return;
            }
            if(!written)
            {
                written = true;
            }
            else
            {
                documentationWriter.AppendLine();
                documentationWriter.Append( " " );
            }
            var lines = value.Split( '\n' );
            var tagWithName = name != null ? tag + $" name=\"{name}\"" : tag;
            if(lines.Length == 1 && !forceElement)
            {
                documentationWriter.Append( $"<{tagWithName}>{value.Trim()}</{tag}>" );
            }
            else
            {
                documentationWriter.AppendLine( $"<{tagWithName}>" );
                foreach(var line in lines)
                {
                    var trimmed = line.Trim();
                    if(!string.IsNullOrEmpty( trimmed ))
                    {
                        documentationWriter.AppendLine( " " + trimmed );
                    }
                }
                documentationWriter.Append( $" </{tag}>" );
            }
        }

        /// <summary>
        /// Writes a comment that documentation is inherited
        /// </summary>
        /// <param name="member">The member to which the documentation tag should be written</param>
        public static void WriteInheritDoc( this CodeTypeMember member )
        {
            member.Comments.Add( new CodeCommentStatement( " <inheritdoc />", true ) );
        }
    }
}

#pragma warning restore S3265 // Non-flags enums should not be used in bitwise operations