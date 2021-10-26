using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Tecan.Sila2.Server;

namespace TestServer.NetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppContext.SetSwitch( "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true );
            if(args.Length > 0 && args[0] == "debug")
            {
                Debugger.Launch();
                args = args.Skip( 1 ).ToArray();
            }

            Bootstrapper.Start( args );
            Console.ReadLine();
        }
    }
}
