using System.Collections.Generic;

namespace Tecan.Sila2.Generator.Test.Dictionary
{
    [SilaFeature("test")]
    public interface IDictionaryService
    {
        IDictionary<int, string> Lookup
        {
            get;
        }
    }
}
