using Contracts;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace Implementation
{
    [Export(typeof( IGreetingService ) )]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class GreetingService : IGreetingService
    {
        public string SayHello( string name )
        {
            if( name != null && name.Any( char.IsDigit ) )
            {
                throw new UnfriendlyNameException("The name contains digits");
            }

            LastGreeted = name;
            return $"Hello, {name}!";
        }

        public string LastGreeted
        {
            get;
            private set;
        }
    }
}
