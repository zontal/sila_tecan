using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
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
