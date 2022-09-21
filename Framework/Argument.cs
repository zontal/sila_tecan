using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2
{
    public static class Argument
    {
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

        public static string Require( IEnumerable<ISilaTransferObject> transferObjects, string name )
        {
            foreach(var item in transferObjects)
            {
                var error = item.GetValidationErrors();
                if(!string.IsNullOrEmpty( error ))
                {
                    return $"{name}: {error}";
                }
            }
            return null;
        }

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

        public static void Validate( IEnumerable<ISilaTransferObject> transferObjects, string name )
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
