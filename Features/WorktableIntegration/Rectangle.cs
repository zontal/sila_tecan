using Tecan.Sila2;

namespace Tecan.Sila2.WorktableIntegration
{
    public struct Rectangle
    {
        public Rectangle(double width, double depth) : this()
        {
            Width = width;
            Depth = depth;
        }

        [Unit("mm", Meter = 1, Factor = 0.001)]
        public double Width { get; }

        [Unit("mm", Meter = 1, Factor = 0.001)]
        public double Depth { get; }
    }
}
