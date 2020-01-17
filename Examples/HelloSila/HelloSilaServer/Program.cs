using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Server;
using Console = System.Console;

namespace HelloSilaServer
{
    class Program
    {
        static void Main( string[] args )
        {
            Bootstrapper.Start( args );
            Console.ReadLine();
        }
    }
}
