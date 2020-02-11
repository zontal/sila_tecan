using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable CheckNamespace

namespace Tecan.Sila2.Generator.Test.TestReference
{
    [SilaFeature]
    public interface IInlineObservableService
    {
        [Observable]
        bool TestInlineObservable( string parameter, CancellationToken cancellationToken );
    }
}
