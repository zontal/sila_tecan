using System;
using System.Collections.Generic;
using System.Reflection;

namespace Tecan.Sila2.ParameterConstraintsProvider
{
    /// <summary>
    /// Defines that the parameter must be constrained to a set whose values are dynamic and are retrieved by a given static method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class SetProviderAttribute : ConstraintProviderAttribute
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="providerType">The type that holds the method to obtain the possible names</param>
        /// <param name="methodName">The name of the public static method</param>
        public SetProviderAttribute( Type providerType, string methodName )
        {
            var method = providerType.GetMethod( methodName, BindingFlags.Public | BindingFlags.Static );
            if( method == null )
            {
                throw new ArgumentException($"The specified method could not be found.");
            }

            Provider = method.CreateDelegate( typeof(Func<IEnumerable<string>>) ) as Func<IEnumerable<string>>;
        }

        /// <summary>
        /// Gets the method that computes the set members
        /// </summary>
        public Func<IEnumerable<string>> Provider
        {
            get;
        }
    }
}
