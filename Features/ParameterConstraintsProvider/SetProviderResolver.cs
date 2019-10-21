using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Tecan.Sila2.ParameterConstraintsProvider
{
    [Export(typeof(IParameterConstraintResolver))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SetProviderResolver : ParameterConstraintResolver<SetProviderAttribute>
    {
        public override ConstrainedType ResolveConstraint( SetProviderAttribute attribute )
        {
            var items = attribute.Provider();
            if( items == null )
            {
                return null;
            }
            return new ConstrainedType()
            {
                Constraints = new Constraints()
                {
                    Set = items.ToArray()
                },
                DataType = new DataTypeType()
                {
                    Item = BasicType.String
                }
            };
        }
    }
}
