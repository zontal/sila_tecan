using Tecan.Sila2;

namespace BlankServer
{
    public class AuthorizationProviderService : Tecan.Sila2.AuthorizationProvider.IAuthorizationProviderService
    {
        [return: SilaIdentifier("TokenLifetime"), Unit("s", Second = 1)]
        public long Verify(string accessToken, [PatternConstraint("[0-9a-f]{8}\\-[0-9a-f]{4}\\-[0-9a-f]{4}\\-[0-9a-f]{4}\\-[0-9a-f]{12}")] string requestedServer, [SilaIdentifierType(IdentifierType.FeatureIdentifier)] string requestedFeature)
        {
            throw new NotImplementedException();
        }
    }
}
