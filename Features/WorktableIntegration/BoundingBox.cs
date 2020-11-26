namespace Tecan.Sila2.WorktableIntegration
{
    /// <summary>
    /// Denotes a bounding box for a device
    /// </summary>
    public struct BoundingBox
    {
        /// <summary>
        /// Creates a new bounding box with the given dimensions
        /// </summary>
        /// <param name="rectangle">The ground rectangle</param>
        /// <param name="height">The height of the bounding box</param>
        public BoundingBox(Rectangle rectangle, double height) : this()
        {
            Rectangle = rectangle;
            Height = height;
        }

        /// <summary>
        /// The ground rectangle
        /// </summary>
        public Rectangle Rectangle { get; }

        /// <summary>
        /// The height of the bounding box
        /// </summary>
        [Unit("mm", Meter = 1, Factor = 0.001)]
        public double Height { get; }
    }
}
