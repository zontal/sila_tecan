using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Tecan.Sila2
{
    /// <summary>
    /// Helper class to check validity of constraints
    /// </summary>
    public static class IdentifierConstraint
    {
        /// <summary>
        /// Determines whether checks of identifier constraints are enabled
        /// </summary>
        public static bool IsEnabled { get; set; } = true;

        private static readonly Regex FeatureIdentifierRegex = new Regex( @"\w+(\.\w+)*\/(\w+(\.\w+)*\/)?\w+(\.\w+)*\/v\d+" );
        private static readonly Regex IdentifierRegex = new Regex( @"[A-Z][a-zA-Z0-9]*" );
        private const string CommandInfix = "/Command/";
        private const string PropertyInfix = "/Property/";
        private const string MetadataInfix = "/Metadata/";
        private const string ErrorInfix = "/DefinedExecutionError/";
        private const string ParameterInfix = "/Parameter/";
        private const string ResponseInfix = "/Response/";
        private const string TypeInfix = "/DataType/";
        private const string IntermediateResponseInfix = "/IntermediateResonse/";

        /// <summary>
        /// Checks whether the given parameter is a valid identifier of the given type
        /// </summary>
        /// <param name="parameter">The parameter</param>
        /// <param name="identifierType">The type of identifier</param>
        /// <param name="parameterName">The name of the parameter</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an identifier type is passed that is not supported</exception>
        public static void CheckParameter( string parameter, IdentifierType identifierType, string parameterName )
        {
            if(!IsValid( parameter, identifierType ))
            {
                throw new ArgumentException( $"{parameter} is not a valid {identifierType}", parameterName );
            }
        }

        /// <summary>
        /// Checks whether the given parameter is a valid identifier of the given type
        /// </summary>
        /// <param name="parameters">The parameters</param>
        /// <param name="identifierType">The type of identifier</param>
        /// <param name="parameterName">The name of the parameter</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an identifier type is passed that is not supported</exception>
        public static void CheckParameters(IEnumerable<string> parameters, IdentifierType identifierType, string parameterName)
        {
            if (parameters != null)
            {
                foreach (string parameter in parameters)
                {
                    if (!IsValid(parameter, identifierType))
                    {
                        throw new ArgumentException($"{parameter} is not a valid {identifierType}", parameterName);
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the given parameter is a valid identifier of the given type
        /// </summary>
        /// <param name="parameter">The parameter</param>
        /// <param name="identifierType">The type of identifier</param>
        /// <returns>True, if the parameter is valid, otherwise False</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an identifier type is passed that is not supported</exception>
        public static bool IsValid( string parameter, IdentifierType identifierType )
        {
            if (!IsEnabled)
            {
                return true;
            }
            switch(identifierType)
            {
                case IdentifierType.FeatureIdentifier:
                    return CheckIdentifier( parameter );
                case IdentifierType.CommandIdentifier:
                    return CheckIdentifier( parameter, CommandInfix );
                case IdentifierType.CommandParameterIdentifier:
                    return CheckIdentifier( parameter, CommandInfix, ParameterInfix );
                case IdentifierType.CommandResponseIdentifier:
                    return CheckIdentifier( parameter, CommandInfix, ResponseInfix );
                case IdentifierType.DefinedExecutionErrorIdentifier:
                    return CheckIdentifier( parameter, ErrorInfix );
                case IdentifierType.PropertyIdentifier:
                    return CheckIdentifier( parameter, PropertyInfix );
                case IdentifierType.TypeIdentifier:
                    return CheckIdentifier( parameter, TypeInfix );
                case IdentifierType.IntermediateResponseIdentifier:
                    return CheckIdentifier( parameter, CommandInfix, IntermediateResponseInfix );
                case IdentifierType.MetadataIdentifier:
                    return CheckIdentifier( parameter, MetadataInfix );
                default:
                    throw new ArgumentOutOfRangeException( nameof( identifierType ) );
            }
        }

        private static bool CheckIdentifier( string input, params string[] terms )
        {
            if(input == null)
            {
                return false;
            }

            var index = 0;
            var featureMatch = FeatureIdentifierRegex.Match( input );
            if(!featureMatch.Success || featureMatch.Index != 0)
            {
                return false;
            }
            index += featureMatch.Length;
            if(terms != null)
            {
                foreach(var term in terms)
                {
                    if(input.IndexOf( term, index ) != index)
                    {
                        return false;
                    }
                    index += term.Length;
                    var identifierMatch = IdentifierRegex.Match( input, index );
                    if(!identifierMatch.Success || identifierMatch.Index != index)
                    {
                        return false;
                    }
                    index += identifierMatch.Length;
                }
            }
            return index == input.Length;
        }
    }
}
