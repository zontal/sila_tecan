using Tecan.Sila2;

namespace Tecan.Sila2.WorktableIntegration
{
    /// <summary>
    /// Denotes a rectangle on the ground
    /// </summary>
    public struct Rectangle
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="width">The width of the rectangle</param>
        /// <param name="depth">The depth of the rectangle</param>
        public Rectangle(double width, double depth) : this()
        {
            Width = width;
            Depth = depth;
        }

        /// <summary>
        /// The width of the rectangle
        /// </summary>
        [Unit("mm", Meter = 1, Factor = 0.001)]
        public double Width { get; }

        /// <summary>
        /// The depth of the rectangle
        /// </summary>
        [Unit("mm", Meter = 1, Factor = 0.001)]
        public double Depth { get; }
    }
}
