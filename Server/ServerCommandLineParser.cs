using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Parses the server command line into <see cref="ServerCommandLineArguments"/> without discovering its options through reflection
    /// </summary>
    /// <remarks>
    /// The parser reproduces what CommandLine.Parser.Default of CommandLineParser 2.9.1 does for
    /// <see cref="ServerCommandLineArguments"/>: the same tokens are accepted, the same values are assigned, the same
    /// errors (instances of the CommandLineParser error types) are reported, and the same help and version screens are
    /// written to the standard error stream. CommandLineParser discovers the options through reflection, which is not
    /// compatible with trimming and Native AOT, so the options are declared here explicitly. They must be kept in sync
    /// with the <see cref="OptionAttribute"/> declarations of <see cref="ServerCommandLineArguments"/>.
    /// </remarks>
    internal static class ServerCommandLineParser
    {
        private const int DefaultMaximumDisplayWidth = 80;
        private const int OptionNamePrefixWidth = 2;
        private const int OptionToHelpTextSeparatorWidth = 4;
        private const int TotalOptionPadding = OptionNamePrefixWidth + OptionToHelpTextSeparatorWidth;

        private static readonly Regex LongNameWithValue = new Regex( "^([^=]+)=([^ ].*)$" );

        private static readonly OptionDefinition[] Options =
        {
            new OptionDefinition( "p", "port", ServerCommandLineArguments.PortHelpText, OptionKind.Int32, ( arguments, value ) => arguments.Port = (int)value ),
            new OptionDefinition( "i", "interface", ServerCommandLineArguments.InterfaceFilterHelpText, OptionKind.Text, ( arguments, value ) => arguments.InterfaceFilter = (string)value ),
            new OptionDefinition( "", "no-discovery", ServerCommandLineArguments.NoDiscoveryHelpText, OptionKind.Switch, ( arguments, value ) => arguments.NoDiscovery = (bool)value ),
            new OptionDefinition( "u", "uuid", ServerCommandLineArguments.ServerUuidHelpText, OptionKind.Guid, ( arguments, value ) => arguments.ServerUuid = (Guid)value ),
            new OptionDefinition( "", "config-name", ServerCommandLineArguments.ConfigNameHelpText, OptionKind.Text, ( arguments, value ) => arguments.ConfigName = (string)value ),
            new OptionDefinition( "c", "certificate", ServerCommandLineArguments.CertificatePathHelpText, OptionKind.Text, ( arguments, value ) => arguments.CertificatePath = (string)value ),
            new OptionDefinition( "n", "name", ServerCommandLineArguments.NameHelpText, OptionKind.Text, ( arguments, value ) => arguments.Name = (string)value ),
        };

        private static readonly OptionDefinition[] HelpScreenEntries = Options.Concat( new[]
        {
            new OptionDefinition( "", "help", "Display this help screen.", OptionKind.Switch, null ),
            new OptionDefinition( "", "version", "Display version information.", OptionKind.Switch, null ),
        } ).ToArray();

        private enum OptionKind
        {
            Int32,
            Text,
            Guid,
            Switch
        }

        /// <summary>
        /// Parses the given command line and either passes the parsed arguments or the errors to the given functions
        /// </summary>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="args">The command line arguments</param>
        /// <param name="parsedFunc">The function that is called with the parsed arguments</param>
        /// <param name="notParsedFunc">The function that is called with the errors, after the help screen was written to the standard error stream</param>
        /// <returns>The result of the called function</returns>
        public static TResult ParseArguments<TResult>( string[] args, Func<ServerCommandLineArguments, TResult> parsedFunc, Func<IEnumerable<Error>, TResult> notParsedFunc )
        {
            var arguments = Parse( args, out var errors );
            if(errors.Count == 0)
            {
                return parsedFunc( arguments );
            }

            Console.Error.Write( BuildHelpText( errors, GetMaximumDisplayWidth(), Assembly.GetEntryAssembly() ) );
            return notParsedFunc( errors );
        }

        /// <summary>
        /// Parses the given command line
        /// </summary>
        /// <param name="args">The command line arguments</param>
        /// <param name="errors">The errors in the command line</param>
        /// <returns>The parsed arguments or null, if the command line contains errors</returns>
        internal static ServerCommandLineArguments Parse( IEnumerable<string> args, out IReadOnlyList<Error> errors )
        {
            if(args == null)
            {
                throw new ArgumentNullException( nameof( args ) );
            }

            var argumentList = args.ToList();
            if(argumentList.Count > 0 && argumentList[0] == "--help")
            {
                errors = new Error[] { CommandLineParserErrors.HelpRequested() };
                return null;
            }

            if(argumentList.Count > 0 && argumentList[0] == "--version")
            {
                errors = new Error[] { CommandLineParserErrors.VersionRequested() };
                return null;
            }

            var found = new List<Error>();
            var tokens = Tokenize( argumentList, found );
            var assignments = Partition( tokens );

            var arguments = new ServerCommandLineArguments();
            var assignedOptions = new List<OptionDefinition>();
            foreach(var option in Options)
            {
                var matched = false;
                string lastValue = null;
                foreach(var assignment in assignments)
                {
                    if(MatchesName( assignment.Key, option ))
                    {
                        matched = true;
                        lastValue = assignment.Value;
                    }
                }

                if(!matched)
                {
                    continue;
                }

                if(option.TryConvert( lastValue, out var value ))
                {
                    option.Assign( arguments, value );
                    assignedOptions.Add( option );
                }
                else
                {
                    found.Add( CommandLineParserErrors.BadFormatConversion( option.ShortName, option.LongName ) );
                }
            }

            found.AddRange( FindRepeatedOptions( tokens, assignedOptions ) );

            var distinctErrors = new List<Error>();
            var seen = new HashSet<string>();
            foreach(var error in found)
            {
                if(seen.Add( error.Tag + "\0" + GetMessage( error ) ))
                {
                    distinctErrors.Add( error );
                }
            }

            errors = distinctErrors;
            return distinctErrors.Count == 0 ? arguments : null;
        }

        /// <summary>
        /// Builds the help screen CommandLineParser writes when the command line contains errors or help or version information was requested
        /// </summary>
        /// <param name="errors">The errors in the command line</param>
        /// <param name="maximumDisplayWidth">The width of the console</param>
        /// <param name="entryAssembly">The assembly whose attributes provide the heading and copyright, or null if there is no entry assembly</param>
        /// <returns>The help screen</returns>
        internal static string BuildHelpText( IReadOnlyCollection<Error> errors, int maximumDisplayWidth, Assembly entryAssembly )
        {
            var assembly = entryAssembly ?? typeof( Parser ).Assembly;
            var heading = GetHeading( assembly );
            if(errors.Any( error => error.Tag == ErrorType.VersionRequestedError ))
            {
                return heading + Environment.NewLine;
            }

            var copyright = GetCopyright( assembly, out var copyrightFailed );
            var preOptions = new StringBuilder();
            var meaningfulErrors = errors
                .Where( error => !error.StopsProcessing )
                .Where( error => !( error is UnknownOptionError unknownOption && string.Equals( unknownOption.Token, "help", StringComparison.OrdinalIgnoreCase ) ) )
                .ToList();
            if(meaningfulErrors.Count > 0)
            {
                // CommandLineParser adds the error section a second time when it fails to build the copyright line
                var errorSections = copyrightFailed ? 2 : 1;
                for(var section = 0; section < errorSections; section++)
                {
                    AddLine( preOptions, Environment.NewLine + "ERROR(S):", maximumDisplayWidth );
                    foreach(var error in meaningfulErrors)
                    {
                        AddLine( preOptions, "  " + GetMessage( error ), maximumDisplayWidth );
                    }
                }
            }

            var license = GetAttribute<AssemblyLicenseAttribute>( assembly );
            if(license != null)
            {
                AddLines( preOptions, license, maximumDisplayWidth );
            }

            var usage = GetAttribute<AssemblyUsageAttribute>( assembly );
            if(usage != null)
            {
                AddLine( preOptions, "USAGE:", maximumDisplayWidth );
                AddLines( preOptions, usage, maximumDisplayWidth );
            }

            var helpText = new StringBuilder( heading );
            if(!string.IsNullOrEmpty( copyright ))
            {
                helpText.Append( Environment.NewLine ).Append( copyright );
            }

            if(preOptions.Length > 0)
            {
                helpText.Append( Environment.NewLine ).Append( preOptions );
            }

            return helpText.Append( Environment.NewLine ).Append( Environment.NewLine ).Append( BuildOptionsText( maximumDisplayWidth ) ).ToString();
        }

        private static List<Token> Tokenize( List<string> arguments, List<Error> errors )
        {
            var tokens = new List<Token>();
            foreach(var argument in arguments)
            {
                if(!argument.StartsWith( "-", StringComparison.Ordinal ))
                {
                    tokens.Add( new Token( false, argument ) );
                }
                else if(argument.StartsWith( "--", StringComparison.Ordinal ))
                {
                    TokenizeLongName( argument, tokens, errors );
                }
                else
                {
                    TokenizeShortName( argument, tokens );
                }
            }

            foreach(var token in tokens)
            {
                if(token.IsName && FindOption( token.Text ) == null)
                {
                    errors.Add( CommandLineParserErrors.UnknownOption( token.Text ) );
                }
            }

            tokens.RemoveAll( token => token.IsName && FindOption( token.Text ) == null );
            return tokens;
        }

        private static void TokenizeLongName( string argument, List<Token> tokens, List<Error> errors )
        {
            if(argument.Length <= 2)
            {
                return;
            }

            var text = argument.Substring( 2 );
            var equalIndex = text.IndexOf( '=' );
            if(equalIndex <= 0)
            {
                tokens.Add( new Token( true, text ) );
                return;
            }

            var match = LongNameWithValue.Match( text );
            if(equalIndex == 1 || !match.Success)
            {
                errors.Add( CommandLineParserErrors.BadFormatToken( argument ) );
                return;
            }

            tokens.Add( new Token( true, match.Groups[1].Value ) );
            tokens.Add( new Token( false, match.Groups[2].Value ) );
        }

        private static void TokenizeShortName( string argument, List<Token> tokens )
        {
            if(argument.Length == 1)
            {
                tokens.Add( new Token( false, argument ) );
                return;
            }

            var text = argument.Substring( 1 );
            if(char.IsDigit( text[0] ))
            {
                tokens.Add( new Token( false, argument ) );
                return;
            }

            if(text.Length == 1)
            {
                tokens.Add( new Token( true, text ) );
                return;
            }

            // the first character is taken as an option name, further characters as names of switches until an
            // unknown character or an option that expects a value; the remainder is that option's value
            var consumed = 0;
            foreach(var character in text)
            {
                var name = new string( character, 1 );
                var option = FindOption( name );
                if(consumed > 0 && option == null)
                {
                    break;
                }

                consumed++;
                tokens.Add( new Token( true, name ) );
                if(option != null && option.Kind != OptionKind.Switch)
                {
                    break;
                }
            }

            if(consumed < text.Length)
            {
                tokens.Add( new Token( false, text.Substring( consumed ) ) );
            }
        }

        private static List<KeyValuePair<string, string>> Partition( List<Token> tokens )
        {
            var switches = new List<string>();
            var scalars = new List<string>();
            var expectingValue = false;
            foreach(var token in tokens)
            {
                if(token.IsName)
                {
                    expectingValue = FindOption( token.Text ).Kind != OptionKind.Switch;
                    ( expectingValue ? scalars : switches ).Add( token.Text );
                }
                else
                {
                    if(expectingValue)
                    {
                        scalars.Add( token.Text );
                    }

                    expectingValue = false;
                }
            }

            // scalar names and values are paired in order; an option name without a value takes the next option
            // name as its value and an unpaired trailing entry is dropped, as CommandLineParser does
            var assignments = switches.Select( name => new KeyValuePair<string, string>( name, "true" ) ).ToList();
            for(var index = 0; index + 1 < scalars.Count; index += 2)
            {
                assignments.Add( new KeyValuePair<string, string>( scalars[index], scalars[index + 1] ) );
            }

            return assignments;
        }

        private static IEnumerable<Error> FindRepeatedOptions( List<Token> tokens, List<OptionDefinition> assignedOptions )
        {
            var names = tokens.Where( token => token.IsName ).Select( token => token.Text ).ToList();
            var occurrences = names.SelectMany( name => assignedOptions.Where( option => option.ShortName == name ) )
                .Concat( names.SelectMany( name => assignedOptions.Where( option => option.LongName == name ) ) );
            return occurrences
                .GroupBy( option => option )
                .Where( group => group.Count() > 1 )
                .Select( group => CommandLineParserErrors.RepeatedOption( group.Key.ShortName, group.Key.LongName ) );
        }

        private static OptionDefinition FindOption( string name )
        {
            return Options.FirstOrDefault( option => MatchesName( name, option ) );
        }

        private static bool MatchesName( string name, OptionDefinition option )
        {
            return name.Length == 1 ? name == option.ShortName : name == option.LongName;
        }

        private static string GetMessage( Error error )
        {
            switch(error)
            {
                case BadFormatTokenError badFormatToken:
                    return "Token '" + badFormatToken.Token + "' is not recognized.";
                case UnknownOptionError unknownOption:
                    return "Option '" + unknownOption.Token + "' is unknown.";
                case BadFormatConversionError badFormatConversion:
                    return "Option '" + badFormatConversion.NameInfo.NameText + "' is defined with a bad format.";
                case RepeatedOptionError repeatedOption:
                    return "Option '" + repeatedOption.NameInfo.NameText + "' is defined multiple times.";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets the width CommandLineParser uses for the help screen: the console width, or 80 if the output is redirected or the width is unknown
        /// </summary>
        /// <returns>The width of the help screen</returns>
        internal static int GetMaximumDisplayWidth()
        {
            if(Console.IsOutputRedirected)
            {
                return DefaultMaximumDisplayWidth;
            }

            try
            {
                var width = Console.WindowWidth;
                return width < 1 ? DefaultMaximumDisplayWidth : width;
            }
            catch(Exception exception) when( exception is IOException || exception is PlatformNotSupportedException || exception is ArgumentOutOfRangeException )
            {
                return DefaultMaximumDisplayWidth;
            }
        }

        private static string GetHeading( Assembly assembly )
        {
            var titleAttribute = GetAttribute<AssemblyTitleAttribute>( assembly );
            var title = titleAttribute != null ? titleAttribute.Title : assembly.GetName().Name;
            var versionAttribute = GetAttribute<AssemblyInformationalVersionAttribute>( assembly );
            var version = versionAttribute != null ? versionAttribute.InformationalVersion : assembly.GetName().Version?.ToString();
            return string.IsNullOrEmpty( version ) ? title : title + " " + version;
        }

        private static string GetCopyright( Assembly assembly, out bool failed )
        {
            failed = false;
            var copyright = GetAttribute<AssemblyCopyrightAttribute>( assembly );
            if(copyright != null)
            {
                return copyright.Copyright;
            }

            var company = GetAttribute<AssemblyCompanyAttribute>( assembly );
            var author = "author";
            if(company != null)
            {
                if(string.IsNullOrWhiteSpace( company.Company ))
                {
                    failed = true;
                }
                else
                {
                    author = company.Company;
                }
            }

            return "Copyright (C) " + DateTime.Now.Year.ToString( CultureInfo.InvariantCulture ) + " " + author;
        }

        private static TAttribute GetAttribute<TAttribute>( Assembly assembly ) where TAttribute : Attribute
        {
            return (TAttribute)assembly.GetCustomAttributes( typeof( TAttribute ), false ).FirstOrDefault();
        }

        private static string BuildOptionsText( int maximumDisplayWidth )
        {
            var maximumNameLength = HelpScreenEntries.Max( entry => entry.HelpScreenName.Length );
            var helpTextWidth = maximumDisplayWidth - ( maximumNameLength + TotalOptionPadding );
            var optionsText = new StringBuilder();
            foreach(var entry in HelpScreenEntries)
            {
                optionsText
                    .Append( ' ', OptionNamePrefixWidth )
                    .Append( entry.HelpScreenName.PadRight( maximumNameLength ) )
                    .Append( ' ', OptionToHelpTextSeparatorWidth )
                    .Append( WrapAndIndentText( entry.HelpText, maximumNameLength + TotalOptionPadding, helpTextWidth ).TrimStart() )
                    .Append( Environment.NewLine )
                    .Append( Environment.NewLine );
            }

            return optionsText.ToString();
        }

        private static void AddLines( StringBuilder builder, MultilineTextAttribute attribute, int maximumDisplayWidth )
        {
            var lines = new[] { attribute.Line1, attribute.Line2, attribute.Line3, attribute.Line4, attribute.Line5 };
            var lineCount = Array.FindLastIndex( lines, line => !string.IsNullOrEmpty( line ) ) + 1;
            for(var index = 0; index < lineCount; index++)
            {
                AddLine( builder, lines[index], maximumDisplayWidth );
            }
        }

        private static void AddLine( StringBuilder builder, string value, int maximumDisplayWidth )
        {
            if(builder.Length > 0)
            {
                builder.Append( Environment.NewLine );
            }

            builder.Append( WrapAndIndentText( value.TrimEnd(), 0, maximumDisplayWidth ) );
        }

        private static string WrapAndIndentText( string input, int indentLevel, int columnWidth )
        {
            columnWidth = Math.Max( 1, columnWidth );
            var indent = new string( ' ', indentLevel );
            var lines = new List<string>();
            foreach(var line in input.Replace( "\r", "" ).Split( '\n' ))
            {
                WordWrapLine( line, columnWidth, lines );
            }

            return string.Join( Environment.NewLine, lines.Select( line => indent + line ) );
        }

        private static void WordWrapLine( string line, int columnWidth, List<string> lines )
        {
            // keeps the indentation of the line and breaks words that do not fit into a line at the column end
            var unindentedLine = line.TrimStart();
            var indentLevel = Math.Min( line.Length - unindentedLine.Length, columnWidth - 1 );
            columnWidth -= indentLevel;
            var wrapped = new List<StringBuilder>();
            foreach(var part in unindentedLine.Split( ' ' ))
            {
                var word = part;
                var previousLength = wrapped.Count > 0 ? wrapped[wrapped.Count - 1].Length : 0;
                if(wrapped.Count > 0 && ( word.Length == 0 || previousLength + word.Length <= columnWidth ))
                {
                    wrapped[wrapped.Count - 1].Append( word ).Append( ' ' );
                    continue;
                }

                do
                {
                    var length = Math.Min( columnWidth, word.Length );
                    wrapped.Add( new StringBuilder( word.Substring( 0, length ) ).Append( ' ' ) );
                    word = word.Substring( length );
                }
                while(word.Length > 0);
            }

            var indent = new string( ' ', indentLevel );
            lines.AddRange( wrapped.Select( builder => indent + builder.ToString().TrimEnd() ) );
        }

        /// <summary>
        /// Creates the error objects CommandLineParser reports, so that error handlers can keep inspecting the
        /// concrete error types such as <see cref="HelpRequestedError"/> or <see cref="UnknownOptionError"/>
        /// </summary>
        /// <remarks>
        /// The constructors of these types are internal to CommandLineParser. They are looked up on statically known
        /// types, which trimming and Native AOT analyze and preserve.
        /// </remarks>
        private static class CommandLineParserErrors
        {
            private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

            public static Error HelpRequested()
            {
                return Create<HelpRequestedError>( typeof( HelpRequestedError ).GetConstructor( NonPublicInstance, null, Type.EmptyTypes, null ) );
            }

            public static Error VersionRequested()
            {
                return Create<VersionRequestedError>( typeof( VersionRequestedError ).GetConstructor( NonPublicInstance, null, Type.EmptyTypes, null ) );
            }

            public static Error UnknownOption( string token )
            {
                return Create<UnknownOptionError>( typeof( UnknownOptionError ).GetConstructor( NonPublicInstance, null, new[] { typeof( string ) }, null ), token );
            }

            public static Error BadFormatToken( string token )
            {
                return Create<BadFormatTokenError>( typeof( BadFormatTokenError ).GetConstructor( NonPublicInstance, null, new[] { typeof( string ) }, null ), token );
            }

            public static Error BadFormatConversion( string shortName, string longName )
            {
                return Create<BadFormatConversionError>( typeof( BadFormatConversionError ).GetConstructor( NonPublicInstance, null, new[] { typeof( NameInfo ) }, null ), CreateNameInfo( shortName, longName ) );
            }

            public static Error RepeatedOption( string shortName, string longName )
            {
                return Create<RepeatedOptionError>( typeof( RepeatedOptionError ).GetConstructor( NonPublicInstance, null, new[] { typeof( NameInfo ) }, null ), CreateNameInfo( shortName, longName ) );
            }

            private static NameInfo CreateNameInfo( string shortName, string longName )
            {
                return Create<NameInfo>( typeof( NameInfo ).GetConstructor( NonPublicInstance, null, new[] { typeof( string ), typeof( string ) }, null ), shortName, longName );
            }

            private static T Create<T>( ConstructorInfo constructor, params object[] arguments )
            {
                if(constructor == null)
                {
                    throw new InvalidOperationException( $"The server command line parser requires CommandLineParser 2.9.1, but {typeof( T ).FullName} of the loaded CommandLineParser version has no matching constructor." );
                }

                return (T)constructor.Invoke( arguments );
            }
        }

        private sealed class Token
        {
            public Token( bool isName, string text )
            {
                IsName = isName;
                Text = text;
            }

            public bool IsName { get; }

            public string Text { get; }
        }

        private sealed class OptionDefinition
        {
            private readonly Action<ServerCommandLineArguments, object> _assign;

            public OptionDefinition( string shortName, string longName, string helpText, OptionKind kind, Action<ServerCommandLineArguments, object> assign )
            {
                ShortName = shortName;
                LongName = longName;
                HelpText = helpText;
                Kind = kind;
                _assign = assign;
            }

            public string ShortName { get; }

            public string LongName { get; }

            public string HelpText { get; }

            public OptionKind Kind { get; }

            public string HelpScreenName => ShortName.Length > 0 ? "-" + ShortName + ", --" + LongName : "--" + LongName;

            public void Assign( ServerCommandLineArguments arguments, object value )
            {
                _assign( arguments, value );
            }

            public bool TryConvert( string text, out object value )
            {
                switch(Kind)
                {
                    case OptionKind.Int32 when int.TryParse( text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number ):
                        value = number;
                        return true;
                    case OptionKind.Guid when Guid.TryParse( text, out var guid ):
                        value = guid;
                        return true;
                    case OptionKind.Switch when bool.TryParse( text, out var flag ):
                        value = flag;
                        return true;
                    case OptionKind.Text:
                        value = text;
                        return true;
                    default:
                        value = null;
                        return false;
                }
            }
        }
    }
}
