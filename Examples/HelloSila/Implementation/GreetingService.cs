using Contracts;
using System;
using System.IO;
using System.Linq;
using Tecan.Sila2;

namespace Implementation
{

    [System.ComponentModel.Composition.ExportAttribute(typeof(IGreetingService))]
    [System.ComponentModel.Composition.PartCreationPolicyAttribute(System.ComponentModel.Composition.CreationPolicy.Shared)]
    public class GreetingService : IGreetingService
    {
        public DemoStructure EchoStructure(DemoStructure structure)
        {
            return structure;
        }

        public Stream EchoStream(Stream stream)
        {
            return stream;
        }

        public string SayHello(string name)
        {
            if (!string.IsNullOrEmpty(name) && name.Any(char.IsDigit))
            {
                throw new UnfriendlyNameException("Name must not contain digits");
            }

            return $"Hello, {name}!";
        }
    }
}
