using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace Tecan.Sila2.Interop.Server
{
    [Export(typeof(IObservablePropertyTest))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ObservablePropertyTest : IObservablePropertyTest
    {
        private readonly DateTime _launched = DateTime.Now;

        public long UpTime => (long)(DateTime.Now - _launched).TotalSeconds;
    }
}
