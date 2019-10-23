using System;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.IntegrationTests
{
    class Program
    {
        static void Main( string[] args )
        {
            Bootstrapper.Start(args);
            Console.ReadLine();
        }
    }
}
