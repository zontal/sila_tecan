using System.Collections.Generic;
using System.Linq;

namespace Tecan.Sila2.DynamicClient
{
    public class DynamicObject
    {
        public IList<DynamicObjectProperty> Elements { get; } = new List<DynamicObjectProperty>();

        public override string ToString()
        {
            return "{ " + string.Join(", ", Elements.Select(p => $"{p.DisplayName} = {Print(p.Value)}")) + " }";
        }

        internal static string Print(object obj)
        {
            if (obj == null)
            {
                return "(null)";
            }

            if (obj is IEnumerable<object> enumerable)
            {
                return "[ " + string.Join(", ", enumerable.Select(Print)) + " ]";
            }
            return obj.ToString();
        }
    }
}
