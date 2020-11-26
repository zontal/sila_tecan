using System;
using NUnit.Framework;

namespace Tecan.Sila2.Generator.Test.Dictionary
{
    [TestFixture]
    public class DictionaryFixture : CodeGeneratorFixtureBase
    {
        protected override string Name => "Dictionary";

        protected override Type Interface => typeof( IDictionaryService );
    }
}
