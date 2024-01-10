using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Specifies the unit of the annotated property or type
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Class)]
    public class UnitAttribute : Attribute
    {
        /// <summary>
        /// Creates a new unit annotation with the given name
        /// </summary>
        /// <param name="label">The name of the unit</param>
        public UnitAttribute(string label)
        {
            Label = label;
        }

        /// <summary>
        /// Gets the name of the unit
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Gets or sets the exponent of the Meter component
        /// </summary>
        public sbyte Meter { get; set; }

        /// <summary>
        /// Gets or sets the exponent of the Kilogram component
        /// </summary>
        public sbyte Kilogram { get; set; }

        /// <summary>
        /// Gets or sets the exponent of the Second component
        /// </summary>
        public sbyte Second { get; set; }

        /// <summary>
        /// Gets or sets the exponent of the Ampere component
        /// </summary>
        public sbyte Ampere { get; set; }
        
        /// <summary>
        /// Gets or sets the exponent of the Kelvin component
        /// </summary>
        public sbyte Kelvin { get; set; }

        /// <summary>
        /// Gets or sets the exponent of the Mole component
        /// </summary>
        public sbyte Mole { get; set; }

        /// <summary>
        /// Gets or sets the exponent of the Candela component
        /// </summary>
        public sbyte Candela { get; set; }

        /// <summary>
        /// Gets or sets the factor of this unit. This defaults to 1
        /// </summary>
        public double Factor { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the offset for this unit. This defaults to zero.
        /// </summary>
        public double Offset { get; set; }
    }
}
