using System;

namespace Tecan.Sila2.ParameterConstraintsProvider
{
    /// <summary>
    /// Implements an abstract constraint resolver for a given type of constraint attribute
    /// </summary>
    /// <typeparam name="T">The type of constraint providing attribute</typeparam>
    public abstract class ParameterConstraintResolver<T> : IParameterConstraintResolver where T : Attribute
    {
        /// <inheritdoc cref="IParameterConstraintResolver"/>
        public Type ConstraintAttributeType => typeof(T);

        /// <inheritdoc cref="IParameterConstraintResolver"/>
        public ConstrainedType ResolveConstraint( Attribute attribute )
        {
            if( attribute is T casted )
            {
                return ResolveConstraint( casted );
            }

            return null;
        }

        /// <summary>
        /// Resolves the constraint specified by the given attribute
        /// </summary>
        /// <param name="attribute">The attribute instance</param>
        /// <returns>A runtime type constraint or null, if no constraint should be applied.</returns>
        public abstract ConstrainedType ResolveConstraint( T attribute );
    }
}
