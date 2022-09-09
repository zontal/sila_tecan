using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Recovery
{
    public enum RecoveryResult
    {
        Retry,
        Abort,
        Throw,
        Continue
    }
}
