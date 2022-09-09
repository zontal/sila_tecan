using System.Collections.Generic;
using System.Linq;

namespace Tecan.Sila2.DynamicClient
{
    /// <summary>
    /// Denotes a dynamic object, i.e. a container whose structure is unclear at compile-time
    /// </summary>
    public class DynamicObject
    {
        /// <summary>
        /// Gets the elements contained in this object
        /// </summary>
        public IList<DynamicObjectProperty> Elements { get; } = new List<DynamicObjectProperty>();

        /// <inheritdoc cref="object"/>
        public override string ToString()
        {
            return "{ " + string.Join( ", ", Elements.Select( p => $"{p.DisplayName} = {Print( p.Value )}" ) ) + " }";
        }

        /// <summary>
        /// Creates the type of this dynamic object on the fly
        /// </summary>
        /// <returns>A SiLA2 structure type with all the elements</returns>
        public DataTypeType CreateType()
        {
            return new DataTypeType
            {
                Item = new StructureType
                {
                    Element = Elements.Select( e => new SiLAElement
                    {
                        Identifier = e.Identifier,
                        DisplayName = e.DisplayName,
                        Description = e.Description,
                        DataType = e.Type
                    } ).ToArray()
                }
            };
        }

        internal static string Print( object obj )
        {
            if(obj == null)
            {
                return "(null)";
            }

            if(obj is IEnumerable<object> enumerable)
            {
                return "[ " + string.Join( ", ", enumerable.Select( Print ) ) + " ]";
            }
            return obj.ToString();
        }
    }
}
