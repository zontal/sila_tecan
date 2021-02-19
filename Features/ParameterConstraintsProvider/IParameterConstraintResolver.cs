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
        ConstrainedType ResolveConstraint( Attribute attribute );
    }

    /// <summary>
    /// Denotes a component that can resolve parameter constraints of a given type and notify changes
    /// </summary>
    public interface INotifyParameterConstraintResolver : IParameterConstraintResolver
    {
        /// <summary>
        /// Subscribes to changes specified by the given attribute
        /// </summary>
        /// <param name="attribute">The attribute instance</param>
        /// <param name="callback">A method that should be called when the constraints represented by this attribute change</param>
        void SubscribeTo( Attribute attribute, Action<ConstrainedType> callback );
    }
}
