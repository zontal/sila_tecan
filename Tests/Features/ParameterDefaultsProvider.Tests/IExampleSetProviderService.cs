using Tecan.Sila2.ParameterConstraintsProvider;

namespace Tecan.Sila2.ParameterDefaultsProvider.Tests
{
    [SilaFeature]
    public interface IExampleSetProviderService
    {
        void ExampleMethod( [SetProvider( typeof(ParameterDefaultsProviderTestsFixture), nameof(ParameterDefaultsProviderTestsFixture.GetPossibleParameters) )]
            string parameter );
    }
}
