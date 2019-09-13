using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Locking
{
    public partial interface ILockController
    {
        [MetadataType(typeof(string))]
        IRequestInterceptor LockIdentifier { get; }
    }
}
