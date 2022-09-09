using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace Tecan.Sila2.Interop.Server.UnobservablePropertyTest
{
    [Export( typeof( IUnobservablePropertyTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class UnobservablePropertyTest : IUnobservablePropertyTest
    {
        public long AnswerToEverything => 42;

        public long SecondsSince1970 => (long)(DateTime.UtcNow - new DateTime( 1970, 1, 1 )).TotalSeconds;
    }
}
