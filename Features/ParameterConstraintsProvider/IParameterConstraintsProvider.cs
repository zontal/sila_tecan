using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.ParameterConstraintsProvider
{
    /// <summary>
    /// This Feature allows a SiLA Client to receive constraints (min./max value, min./max. length for strings etc.) that
    /// given parameters of a given Command have which are not defined in the respective Feature Definition;
    /// also depending on other parameter or state.
    /// 
    /// It returns a list of all Constraints of a given Command. It is RECOMMENDED that a SiLA Server implements this
    /// feature, if it is not possible to provide certain constraints already in the feature design phase.
    /// </summary>
    [SilaFeature(false, "core")]
    public interface IParameterConstraintsProvider
    {
        /// <summary>
        /// Provides a list of all constraints of Command Parameters of the SiLA Server.
        /// </summary>
        ICollection<ParameterConstraint> ParametersConstraints
        {
            get;
        }
    }
}
