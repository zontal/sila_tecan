using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes a class used to support the generated code with argument validation
    /// </summary>
    public static class Argument
    {
        /// <summary>
        /// Returns an error message if given transfer object is missing or has errors
        /// </summary>
        /// <param name="transferObject">The transfer object to validate</param>
        /// <param name="name">The member name</param>
        /// <returns>An error message which is blank in case there are no problems</returns>
        public static string Require( ISilaTransferObject transferObject, string name )
        {
            if(transferObject == null)
            {
                return $"{name} is required but missing";
            }
            var error = transferObject.GetValidationErrors();
            if(string.IsNullOrEmpty( error ))
            {
                return error;
            }
            else
            {
                return $"{name}: {error}";
            }
        }

        /// <summary>
        /// Returns an error message if given transfer objects have errors
        /// </summary>
        /// <param name="transferObjects">The transfer objects to validate</param>
        /// <param name="name">The member name</param>
        /// <returns>An error message which is blank in case there are no problems</returns>
        public static string Require( IEnumerable<ISilaTransferObject> transferObjects, string name )
        {
            if(transferObjects != null)
            {
                foreach(var item in transferObjects)
                {
                    var error = item.GetValidationErrors();
                    if(!string.IsNullOrEmpty( error ))
                    {
                        return $"{name}: {error}";
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Validates that the given transfer object is present and does not contain errors
        /// </summary>
        /// <param name="transferObject">The transfer object</param>
        /// <param name="name">The member name</param>
        /// <exception cref="ArgumentException">Throws an argument exception with the parameter name provided</exception>
        public static void Validate( ISilaTransferObject transferObject, string name )
        {
            if(transferObject == null)
            {
                throw new ArgumentException( $"{name} is required but missing", ToCamelCase( name ) );
            }
            var error = transferObject.GetValidationErrors();
            if(!string.IsNullOrEmpty( error ))
            {
                throw new ArgumentException( error, ToCamelCase( name ) );
            }
        }

        /// <summary>
        /// Validates that the given transfer objects are present and do not contain errors
        /// </summary>
        /// <param name="transferObjects">The transfer objects</param>
        /// <param name="name">The member name</param>
        /// <exception cref="ArgumentException">Throws an argument exception with the parameter name provided</exception>
        public static void Validate( IEnumerable<ISilaTransferObject> transferObjects, string name )
        {
            if(transferObjects != null)
            {
                foreach(var item in transferObjects)
                {
                    var error = item.GetValidationErrors();
                    if(!string.IsNullOrEmpty( error ))
                    {
                        throw new ArgumentException( error, ToCamelCase( name ) );
                    }
                }
            }
        }

        private static string ToCamelCase( string name )
        {
            if(string.IsNullOrEmpty( name ))
            {
                return name;
            }
            return char.ToLowerInvariant( name[0] ) + name.Substring( 1 );
        }
    }
}
