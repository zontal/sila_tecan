using System.Threading;

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
