using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Interop.Server
{
    public partial interface IComplexDataTypeTest
    {
        IRequestInterceptor User
        {
            get;
        }
    }
}
