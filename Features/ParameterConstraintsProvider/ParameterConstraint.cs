using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.ParameterConstraintsProvider
{
    /// <summary>
    /// Denotes a runtime constraint to a given SiLA2 command parameter
    /// </summary>
    public struct ParameterConstraint
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="commandParameterIdentifier">The fully qualified identifier of the command parameter</param>
        /// <param name="constraints">The constraints that apply to this command parameter</param>
        public ParameterConstraint( string commandParameterIdentifier, string constraints ) : this()
        {
            CommandParameterIdentifier = commandParameterIdentifier;
            Constraints = constraints;
        }

        /// <summary>
        /// The fully qualified identifier of the command parameter
        /// </summary>
        [SilaIdentifierType(IdentifierType.CommandParameterIdentifier)]
        public string CommandParameterIdentifier
        {
            get;
        }

        /// <summary>
        /// The constraints that apply to this command parameter
        /// </summary>
        [Schema( "https://gitlab.com/SiLA2/sila_base/raw/master/schema/Constraints.xsd", SchemaType.Xml )]
        public string Constraints
        {
            get;
        }
    }
}
