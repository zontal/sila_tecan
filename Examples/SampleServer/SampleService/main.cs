using System.Threading;
using Tecan.Sila2.Server;

namespace SampleService
{
    class Program
    {
        static void Main(string[] args)
        {
            Logging.SetupCommonLogging();

            Bootstrapper.Start(args);
            new EventWaitHandle(false, EventResetMode.AutoReset).WaitOne();
        }
    }
}
