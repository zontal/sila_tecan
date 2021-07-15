using System;
using System.Threading;

// ReSharper disable CheckNamespace

namespace Tecan.Sila2.Generator.Test.TestReference
{
    [SilaFeature]
    public interface IRxService
    {
        IObservable<bool> RunRxObservable();
    }
}
