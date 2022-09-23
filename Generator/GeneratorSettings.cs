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
    }
}
