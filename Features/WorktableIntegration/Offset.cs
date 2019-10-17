using Tecan.Sila2;

namespace Tecan.Sila2.WorktableIntegration
{
    /// <summary>
    /// Denotes a positional offset
    /// </summary>
    public struct Offset
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="x">The X offset</param>
        /// <param name="y">The Y offset</param>
        /// <param name="z">The Z offset</param>
        public Offset(double x, double y, double z) : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// The X offset
        /// </summary>
        [Unit("mm", Meter = 1, Factor = 0.001)]
        public double X { get; }

        /// <summary>
        /// The Y offset
        /// </summary>
        [Unit("mm", Meter = 1, Factor = 0.001)]
        public double Y { get; }

        /// <summary>
        /// The Z offset
        /// </summary>
        [Unit("mm", Meter = 1, Factor = 0.001)]
        public double Z { get; }
    }
}
