using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Generator.Test.TestReference
{
    public partial interface ILoggingService
    {
        IRequestInterceptor LoggingActivity
        {
            get;
        }
    }
}
