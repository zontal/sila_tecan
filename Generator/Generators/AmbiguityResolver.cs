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
    [Export(typeof(IAmbiguityResolver))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class AmbiguityResolver : IAmbiguityResolver
    {
        /// <summary>
        /// Get property name for ambiguous parameter and type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameterName"></param>
        /// <param name="AllowedPropertyOptions">Allowed options for user input</param>
        /// <returns>Property name for ambiguous parameter and type, if null wrong input from user</returns>
        public MemberInfo GetUserDefinedProperty( Type type, string parameterName, IReadOnlyList<MemberInfo> AllowedPropertyOptions )
        {
            string allowedOptions = string.Join( "\n", AllowedPropertyOptions.Select( ( x, index ) => $"{index} - {x.Name} ({x.MemberType}))" ) );
            Console.WriteLine( $"Parameter name '{parameterName}' cannot be resolved to a property or field name of type '{type.Name}'. Please select one of the fields below." );
            Console.WriteLine( allowedOptions );
            var userDefinedProperty = Console.ReadLine();
            MemberInfo selection = null;
            while(selection == null)
            {
                if(int.TryParse( userDefinedProperty, out int index ))
                {
                    if(AllowedPropertyOptions.Count > index && index >= 0)
                    {
                        return AllowedPropertyOptions.ElementAt( index );
                    }
                }
                selection = AllowedPropertyOptions.FirstOrDefault( x => userDefinedProperty.Equals( x.Name, StringComparison.OrdinalIgnoreCase ) );
            }
            return selection;
        }
    }
}
