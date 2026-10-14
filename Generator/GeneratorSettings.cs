using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Generator
{
    /// <summary>
    /// Denotes common code generator runtime settings
    /// </summary>
    public class GeneratorSettings
    {
        /// <summary>
        /// Gets or sets whether the code generator can use interactive behavior
        /// </summary>
        public static bool IsInteractive
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets whether the generated code should serialize data transfer objects through the reflection-based
        /// protobuf-net runtime model instead of a compile-time generated type model
        /// </summary>
        public static bool UseRuntimeTypeModel
        {
            get;
            set;
        }
    }
}
