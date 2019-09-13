using Tecan.Sila2;

namespace Tecan.Sila2.WorktableIntegration
{
    public struct Offset
    {
        public Offset(double x, double y, double z) : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        [Unit("mm", Meter = 1, Factor = 0.001)]
        public double X { get; }

        [Unit("mm", Meter = 1, Factor = 0.001)]
        public double Y { get; }

        [Unit("mm", Meter = 1, Factor = 0.001)]
        public double Z { get; }
    }
}
