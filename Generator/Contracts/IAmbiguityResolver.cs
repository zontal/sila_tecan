using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Denotes an inteface for resolving ambiguity
    /// </summary>
    public interface IAmbiguityResolver
    {
        /// <summary>
        /// Get property name for ambiguous parameter and type
        /// </summary>
        /// <returns>Property name for ambiguous parameter and type, if null wrong input from user</returns>
        public MemberInfo GetUserDefinedProperty( Type type, string parameterName, IReadOnlyList<MemberInfo> allowedPropertyOptions );

        /// <summary>
        /// Selects the overload that is going to be exposed via SiLA2
        /// </summary>
        /// <param name="overloads">The overloads available</param>
        /// <returns>The overload that should be exposed to SiLA2 or null, if the method should not be exposed at all.</returns>
        public MethodInfo GetExposedOverload( IReadOnlyList<MethodInfo> overloads );
    }
}
