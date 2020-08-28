using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Denotes an inteface for Resolving ambiguity
    /// </summary>
    public interface IAmbiguityResolver
    {
        /// <summary>
        /// Get property name for ambiguous parameter and type
        /// </summary>
        /// <returns>Property name for ambiguous parameter and type, if null wrong input from user</returns>
        public string GetUserDefinedProperty(string questionForUser, IReadOnlyList<MemberInfo> allowedPropertyOptions);
    }
}
