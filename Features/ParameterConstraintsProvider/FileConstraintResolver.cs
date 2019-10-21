using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;

namespace Tecan.Sila2.ParameterConstraintsProvider
{
    [Export(typeof(IParameterConstraintResolver))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class FileConstraintResolver : ParameterConstraintResolver<FileConstraintAttribute>
    {
        public override ConstrainedType ResolveConstraint( FileConstraintAttribute attribute )
        {
            return new ConstrainedType()
            {
                Constraints = new Constraints()
                {
                    Set = Directory.GetFiles( attribute.Folder, attribute.Filter, SearchOption.TopDirectoryOnly ).Select( Path.GetFileName ).ToArray()
                },
                DataType = new DataTypeType()
                {
                    Item = BasicType.String
                }
            };
        }
    }
}
