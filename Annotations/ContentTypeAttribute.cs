using System;
using System.Collections.Generic;
using System.Linq;

namespace Tecan.Sila2
{
    /// <summary>
    /// Restricts the content type of the given property
    /// </summary>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue )]
    public class ContentTypeAttribute : Attribute
    {
        /// <summary>
        /// The type of the content
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// The subtype of the content
        /// </summary>
        public string SubType { get; }

        /// <summary>
        /// Gets the parameters of the content type
        /// </summary>
        public Dictionary<string, string> Parameters { get; }


        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="type">The type of the content</param>
        /// <param name="subType">The subtype of the content</param>
        public ContentTypeAttribute( string type, string subType )
        {
            Type = type;
            SubType = subType;
        }


        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="type">The type of the content</param>
        /// <param name="subType">The subtype of the content</param>
        /// <param name="parameters">The parameters of the content type</param>
        public ContentTypeAttribute( string type, string subType, params string[] parameters ) : this( type, subType )
        {
            if(parameters != null)
            {
                var parameterDict = new Dictionary<string, string>();
                foreach(var parameter in parameters)
                {
                    var delemiter = parameter.IndexOf( '=' );
                    if(delemiter != -1)
                    {
                        var key = parameter.Substring( 0, delemiter );
                        var value = parameter.Substring( delemiter + 1 );
                        parameterDict[key] = value;
                    }
                    else
                    {
                        throw new InvalidOperationException( $"The parameter specification '{parameter}' is invalid. Please separate key and value with a '=' character." );
                    }
                }
                Parameters = parameterDict;
            }
        }
    }
}
