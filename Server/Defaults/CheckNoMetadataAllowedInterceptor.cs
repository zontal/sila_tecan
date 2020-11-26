using System.ComponentModel.Composition;

namespace Tecan.Sila2.Server
{
    [Export(typeof(IRequestInterceptor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CheckNoMetadataAllowedInterceptor : IRequestInterceptor
    {
        public int Priority => 1;

        public bool AppliesToCommands => true;

        public bool AppliesToProperties => true;

        public bool IsInterceptRequired(Feature feature, string commandIdentifier)
        {
            return feature.FullyQualifiedIdentifier == "sila2/org/silastandard/core/silaservice/v1/SiLAService";
        }

        public IRequestInterception Intercept(string commandIdentifier, ISiLAServer server, IMetadataRepository metadata)
        {
            if (metadata.Count > 0)
            {
                throw ServerErrorHandling.CreateFrameworkError(FrameworkErrorType.NoMetadataAllowed);
            }
            return null;
        }
    }
}
