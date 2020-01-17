using System;
using System.IO;
using Tecan.Sila2;

namespace Contracts
{
    [SilaFeature("demo")]
    [SilaDisplayName("Greeting service")]
    [SilaDescription("Service to greet people")]
    public interface IGreetingService
    {
        [SilaDescription("Says hello to people")]
        [return: SilaDescription("A greeting")]
        [Throws(typeof(UnfriendlyNameException))]
        string SayHello( [SilaDescription("The name of the person greeted")] string name );

        [SilaDescription("Gets the name of the last person that was greeted")]
        string LastGreeted
        {
            get;
        }
    }

    [SilaDescription("This exception is thrown if a name is considered an attempted attack")]
    public class UnfriendlyNameException : Exception
    {
        public UnfriendlyNameException( string message ) : base(message)
        {
        }
    }
}
