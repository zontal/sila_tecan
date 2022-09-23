using Common.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generate object for ambiguity resolving
    /// </summary>
    [Export( typeof( IAmbiguityResolver ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    public class AmbiguityResolver : IAmbiguityResolver
    {
        private static readonly ILog _log = LogManager.GetLogger( typeof( AmbiguityResolver ) );

        /// <summary>
        /// Selects the overload that is going to be exposed via SiLA2
        /// </summary>
        /// <param name="overloads">The overloads available</param>
        /// <returns>The overload that should be exposed to SiLA2 or null, if the method should not be exposed at all.</returns>
        public MethodInfo GetExposedOverload( IReadOnlyList<MethodInfo> overloads )
        {
            var index = Select( $"Method {overloads[0].Name} is overloaded but SiLA2 does not support overloading. Which overload should be exposed?", overloads.Select( ov => string.Join( ", ", ov.GetParameters().Select( p => p.ParameterType.Name ) ) ).ToArray(), true );
            if(index < overloads.Count && index >= 0)
            {
                return overloads[index];
            }
            return null;
        }

        /// <summary>
        /// Get property name for ambiguous parameter and type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameterName"></param>
        /// <param name="allowedPropertyOptions">Allowed options for user input</param>
        /// <returns>Property name for ambiguous parameter and type, if null wrong input from user</returns>
        public MemberInfo GetUserDefinedProperty( Type type, string parameterName, IReadOnlyList<MemberInfo> allowedPropertyOptions )
        {
            var index = Select( $"Parameter name '{parameterName}' cannot be resolved to a property or field name of type '{type.Name}'. Which property shall be used?", allowedPropertyOptions.Select( p => p.Name ).ToArray(), true );
            if(index < allowedPropertyOptions.Count && index >= 0)
            {
                return allowedPropertyOptions[index];
            }
            return null;
        }

        private int Select( string question, string[] options, bool allowNull )
        {
            if(!GeneratorSettings.IsInteractive)
            {
                _log.Warn( question + " However, the process is non-interactive, so default option is chosen. " );
                return allowNull ? options.Length : 0;
            }
            string allowedOptions = string.Join( "\n", options.Select( ( x, index ) => $"{index} - {x}" ) );
            Console.WriteLine( question + " Please select one of the options below." );
            Console.WriteLine( allowedOptions );
            if(allowNull)
            {
                Console.WriteLine( $"{options.Length} - Skip" );
            }
            var selection = -1;
            while(true)
            {
                var input = Console.ReadLine();
                if(int.TryParse( input, out int index ))
                {
                    if(options.Length > index && index >= 0)
                    {
                        return index;
                    }
                    if(index == options.Length && allowNull)
                    {
                        return options.Length;
                    }
                }
                selection = Array.BinarySearch( options, input );
                if(selection < 0)
                {
                    Console.WriteLine( $"'{input}' was not understood, please type either the number or the option shown above" );
                }
                else
                {
                    return selection;
                }
            }
        }
    }
}
