using System;
using Tecan.Sila2.Server;

namespace SampleService
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Sila2 Server - press <Return> to stop");
            Bootstrapper.Start(args, new Tecan.Sila2.Logging.NLogLogging());
            Console.ReadLine();
        }
    }
}
