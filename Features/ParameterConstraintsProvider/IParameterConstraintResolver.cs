using System;

namespace Tecan.Sila2.ParameterConstraintsProvider
{
    /// <summary>
    /// Denotes a component that can resolve parameter constraints of a given type
    /// </summary>
    public interface IParameterConstraintResolver
    {
        /// <summary>
        /// The type of constraint provider attributes that this component can resolve
        /// </summary>
        Type ConstraintAttributeType
        {
            get;
        }

        /// <summary>
        /// Resolves the constraint specified by the given attribute
        /// </summary>
        /// <param name="attribute">The attribute instance</param>
        /// <returns>A runtime type constraint or null, if no constraint should be applied.</returns>
        ConstrainedType ResolveConstraint( ConstraintProviderAttribute attribute );
    }
}
