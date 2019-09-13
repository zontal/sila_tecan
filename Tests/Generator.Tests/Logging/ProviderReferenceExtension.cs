using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Generator.Test.TestReference
{
    public sealed partial class LoggingServiceProvider
    {
        partial class Client
        {
            public IRequestInterceptor LoggingActivity => null;
        }
    }
}
