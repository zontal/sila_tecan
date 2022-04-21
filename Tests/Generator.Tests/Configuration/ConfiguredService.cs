using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Generator.Test.TestReference
{
    public interface IConfiguredService
    {
        Speed GetSpeed();

        void AccelerateTo( Speed target, [Unit( "m/s²", Meter = 1, Second = -2 )] double maxAcceleration, TimeSpan timeRange );
    }

    public struct Speed
    {
        public double Kmph
        {
            get;
        }

        public Speed( double kmph )
        {
            Kmph = kmph;
        }
    }
}
