using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes helper functions for regex validation
    /// </summary>
    public class RegexConstraint
    {
        private static readonly ConcurrentDictionary<string, Regex> _regexes = new ConcurrentDictionary<string, Regex>();

        /// <summary>
        /// Determines whether the checks for regular expressions are enabled or not
        /// </summary>
        public static bool IsEnabled
        {
            get; set;
        } = true;

        /// <summary>
        /// Determines whether the given input string is a match for the given pattern
        /// </summary>
        /// <param name="input">The input provided by the user</param>
        /// <param name="pattern">The pattern that should be followed</param>
        /// <returns>True, if the input is a match, otherwise False</returns>
        /// <exception cref="ArgumentNullException">Thrown if the pattern is null</exception>
        public static bool IsMatch( string input, string pattern )
        {
            if(pattern == null)
            {
                throw new ArgumentNullException( nameof( pattern ) );
            }

            if(input == null || !IsEnabled)
            {
                return false;
            }

            var regex = _regexes.GetOrAdd( pattern, CreateRegex );
            return regex.IsMatch( input );
        }

        private static Regex CreateRegex( string regex )
        {
            return new Regex( regex, RegexOptions.Compiled );
        }
    }
}
