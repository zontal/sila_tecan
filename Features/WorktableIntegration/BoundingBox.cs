using Tecan.Sila2;

namespace Tecan.Sila2.WorktableIntegration
{
    public struct BoundingBox
    {
        public BoundingBox(Rectangle rectangle, double height) : this()
        {
            Rectangle = rectangle;
            Height = height;
        }

        public Rectangle Rectangle { get; }

        [Unit("mm", Meter = 1, Factor = 0.001)]
        public double Height { get; }
    }
}
