using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

// ReSharper disable CheckNamespace

namespace Tecan.Sila2.Generator.Test.TestReference
{
    [SilaFeature]
    public interface ICentrifuge
    {
        int Speed { get; set; }

        void RunCrazy( ICollection<int> speeds );
    }
}
