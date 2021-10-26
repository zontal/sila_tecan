using System;
using System.Diagnostics;
using System.Linq;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.IntegrationTests
{
    class Program
    {
        static void Main( string[] args )
        {
            if( args.Length > 0 && args[0] == "debug" )
            {
                Debugger.Launch();
                args = args.Skip( 1 ).ToArray();
            }
            
            Bootstrapper.Start(args);
            Console.ReadLine();
        }
    }
}
