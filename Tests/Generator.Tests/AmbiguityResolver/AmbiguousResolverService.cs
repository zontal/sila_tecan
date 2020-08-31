using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Generator.Test.AmbiguityResolver
{
    [SilaFeature]
    public interface IAmbiguityResolverService
    {
        [Observable]
        AmbiguousClass CheckAmbiguousEntries();
    }

    [SilaDescription("Describes a class which has ambiguous parameter")]
    public class AmbiguousClass
    {
        public AmbiguousClass(string message, string channel)
        {
            Message = message;
            AmbiguousChannel = channel;
        }

        public string Message { get; }

        public string AmbiguousChannel { get; }
    }

}
