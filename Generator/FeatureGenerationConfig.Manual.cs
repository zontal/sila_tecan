using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Generator
{
    public partial class FeatureSpec
    {
        /// <summary>
        /// Attempts to obtain the property specification for the given property
        /// </summary>
        /// <param name="member">The name of the property</param>
        /// <param name="spec">The spec</param>
        /// <returns>True, if a spec could be found, otherwise null</returns>
        public bool TryGetPropertyFor( string member, out PropertySpec spec )
        {
            if(Property != null)
            {
                spec = Property.FirstOrDefault( p => p.Code == member );
                return spec != null;
            }

            spec = null;
            return false;
        }

        /// <summary>
        /// Attempts to obtain the command specification for the given command
        /// </summary>
        /// <param name="member">The name of the command</param>
        /// <param name="spec">The spec</param>
        /// <returns>True, if a spec could be found, otherwise null</returns>
        public bool TryGetCommandFor( string member, out CommandSpec spec )
        {
            if(Command != null)
            {
                spec = Command.FirstOrDefault( p => p.Code == member );
                return spec != null;
            }

            spec = null;
            return false;
        }

        /// <summary>
        /// Attempts to obtain the type specification for the given type
        /// </summary>
        /// <param name="type">The name of the type</param>
        /// <param name="spec">The spec</param>
        /// <returns>True, if a spec could be found, otherwise null</returns>
        public bool TryGetTypeFor( string type, out TypeSpec spec )
        {
            if(Type != null)
            {
                spec = Type.FirstOrDefault( p => p.Code == type );
                return spec != null;
            }

            spec = null;
            return false;
        }
    }

    public partial class TypeSpec
    {
        /// <summary>
        /// Attempts to obtain the parameter mapping for the given parameter
        /// </summary>
        /// <param name="parameter">The name of the property</param>
        /// <param name="mapping">The mapping</param>
        /// <returns>True, if a mapping could be found, otherwise null</returns>
        public bool TryGetMappingFor( string parameter, out PropertyMapping mapping )
        {
            if(Property != null)
            {
                mapping = Property.FirstOrDefault( p => p.Key == parameter );
                return mapping != null;
            }

            mapping = null;
            return false;
        }
    }
}
