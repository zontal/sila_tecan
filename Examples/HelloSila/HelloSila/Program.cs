using System;
using Tecan.Sila2.Server;

namespace HelloSila
{
    class Program
    {
        static void Main(string[] args)
        {
            Bootstrapper.Start(args);
            Console.ReadLine();
        }
    }
}
