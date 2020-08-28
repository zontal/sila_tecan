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
    public class AmbiguityResolver : IAmbiguityResolver
    {
        /// <summary>
        /// Get property name for ambiguous parameter and type
        /// </summary>
        /// <param name="QuestionForUser">Questions to show in console</param>
        /// <param name="AllowedPropertyOptions">Allowed options for user input</param>
        /// <returns>Property name for ambiguous parameter and type, if null wrong input from user</returns>
        public string GetUserDefinedProperty(string QuestionForUser, IReadOnlyList<MemberInfo> AllowedPropertyOptions)
        {
            string allowedOptions = string.Join(",", AllowedPropertyOptions.Select(x=>x.Name));
            Console.WriteLine(QuestionForUser);
            Console.WriteLine(allowedOptions);
            var userDefinedProperty = Console.ReadLine();
            if (AllowedPropertyOptions.Count(x => userDefinedProperty.Equals(x.Name, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                return userDefinedProperty;
            }
            return null;
        }
    }
}
