using Tecan.Sila2.ParameterConstraintsProvider;

namespace Tecan.Sila2.ParameterDefaultsProvider.Tests
{
    [SilaFeature]
    public interface IExampleFileConstraintProviderService
    {
        void FileExampleMethod( [FileConstraint("TestData", "*.txt")]
            string parameter );
    }
}
