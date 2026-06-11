using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Tecan.Sila2.AuthorizationProvider;
using Tecan.Sila2.Discovery;
using AuthorizationInvalidAccessTokenException = Tecan.Sila2.Authorization.InvalidAccessTokenException;

namespace Tecan.Sila2.Authentication.Tests
{
    [TestFixture]
    public class AuthenticationTestsFixture
    {
        private AuthenticationAuthorizationService _authService;
        private string _serverId;
        private Mock<ISiLAService> _serverMock;
        private Mock<IAuthConfigurationProvider> _configProviderMock;
        private Mock<IServerConnector> _serverConnectorMock;

        [SetUp]
        public void InitializeTest()
        {
            _serverMock = new Mock<ISiLAService>( MockBehavior.Strict );
            _serverId = Guid.NewGuid().ToString();
            _serverMock.Setup( s => s.ServerUUID ).Returns( _serverId );

            _configProviderMock = new Mock<IAuthConfigurationProvider>( MockBehavior.Strict );
            _configProviderMock.Setup( p => p.Login( It.IsAny<string>(), It.IsAny<string>() ) )
                .Returns( null as User );
            _configProviderMock.Setup( p => p.Login( "testadmin", "testadmin-pwd" ) )
                .Returns( new User()
                {
                    AllowWildcard = true,
                    Name = "testadmin",
                    Password = "testadmin-pwd"
                } );
            _configProviderMock.Setup( p => p.Login( "test", "test-pwd" ) )
                .Returns( new User()
                {
                    AllowWildcard = false,
                    Name = "test",
                    Password = "test-pwd",
                    Permission = new[]
                    {
                        "org.silastandard/core/SiLAService/v1"
                    }
                } );

            _serverConnectorMock = new Mock<IServerConnector>( MockBehavior.Strict );

            _authService = new AuthenticationAuthorizationService( new Lazy<ISiLAService>( () => _serverMock.Object ), _configProviderMock.Object );
        }

        [TestCase( "test", "test-pwd", "org.silastandard/core/SiLAService/v1" )]
        [TestCase( "testadmin", "testadmin-pwd", "org.silastandard/core/SiLAService/v1" )]
        public void AuthenticateRequest_RequestFeature_Succeeds( string userName, string password, string feature )
        {
            var token = _authService.Login( userName, password, _serverId, new List<string>() { feature } );
            Assert.That( token.AccessToken, Is.Not.Null );
            Assert.That( token.TokenLifetime, Is.GreaterThan( 3600L ) );

            _configProviderMock.Setup(p => p.IsValidApiKey(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            var interception = _authService.Intercept( "org.silastandard/core/SiLAService/v1/Command/GetFeatureDefinition", token.AccessToken );
            Assert.That( interception, Is.Null );
            Assert.DoesNotThrow((NUnit.Framework.TestDelegate)(() => _authService.Logout( token.AccessToken ) ));
        }

        [TestCase( "test", "test-pwd", "org.silastandard/core/SiLAService/v1", "org.silastandard/core/LockController/v1/Command/LockServer" )]
        [TestCase( "testadmin", "testadmin-pwd", "org.silastandard/core/SiLAService/v1", "org.silastandard/core/LockController/v1/Command/LockServer" )]
        public void AuthenticateRequest_RequestDifferentFeature_ThrowsException( string userName, string password, string tokenFeature, string requestedCommand )
        {
            var tokenFeatures = new List<string>();
            if(tokenFeature != null)
            {
                tokenFeatures.Add( tokenFeature );
            }

            var token = _authService.Login( userName, password, _serverId, tokenFeatures );
            _configProviderMock.Setup(p => p.IsValidApiKey(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            Assert.Throws<AuthorizationInvalidAccessTokenException>((NUnit.Framework.TestDelegate)(() => _authService.Intercept( requestedCommand, token.AccessToken ) ));
            Assert.DoesNotThrow((NUnit.Framework.TestDelegate)(() => _authService.Logout( token.AccessToken ) ));
        }

        [Test]
        public void AuthenticateRequest_BullshitToken_ThrowsException()
        {
            _configProviderMock.Setup( p => p.AuthorizationProvider ).Returns( null as IAuthorizationProviderService );
            _configProviderMock.Setup( p => p.IsValidApiKey(It.IsAny<string>(), It.IsAny<string>())).Returns( false );
            Assert.Throws<AuthorizationInvalidAccessTokenException>((NUnit.Framework.TestDelegate)(() => _authService.Intercept( "org.silastandard/core/SiLAService/v1/Command/GetFeatureDefinition", Guid.NewGuid().ToString() ) ));
        }

        [Test]
        public void AuthenticateRequest_NoToken_ThrowsException()
        {
            Assert.Throws<AuthorizationInvalidAccessTokenException>((NUnit.Framework.TestDelegate)(() => _authService.Intercept( "org.silastandard/core/SiLAService/v1/Command/GetFeatureDefinition", null ) ));
        }

        [TestCase( "testadmin", "testadmin-pwd" )]
        public void AuthenticateRequest_Wildcard_Succeeds( string userName, string password )
        {
            var token = _authService.Login( userName, password, _serverId, new List<string>() );
            Assert.That( token.AccessToken, Is.Not.Null );
            Assert.That( token.TokenLifetime, Is.GreaterThan( 3600L ) );

            _configProviderMock.Setup(p => p.IsValidApiKey(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            var interception = _authService.Intercept( "org.silastandard/core/SiLAService/v1/Command/GetFeatureDefinition", token.AccessToken );
            Assert.That( interception, Is.Null );
            Assert.DoesNotThrow((NUnit.Framework.TestDelegate)(() => _authService.Logout( token.AccessToken ) ));
        }

        [Test]
        public void AuthenticateRequest_FromAuthProvider_Succeeds()
        {
            var accessToken = Guid.NewGuid().ToString();

            var providerMock = new Mock<IAuthorizationProviderService>( MockBehavior.Strict );
            providerMock.Setup( p => p.Verify( accessToken, _serverId, "org.silastandard/core/SiLAService/v1" ) ).Returns( 3600 );
            _configProviderMock.Setup( p => p.AuthorizationProvider ).Returns( providerMock.Object );
            _configProviderMock.Setup(p => p.IsValidApiKey(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            var interception = _authService.Intercept( "org.silastandard/core/SiLAService/v1/Command/GetFeatureDefinition", accessToken );
            Assert.That( interception, Is.Null );
        }

        [Test]
        public void AuthenticateRequest_FromAuthProviderExpired_Succeeds()
        {
            var accessToken = Guid.NewGuid().ToString();

            var providerMock = new Mock<IAuthorizationProviderService>( MockBehavior.Strict );
            providerMock.Setup( p => p.Verify( accessToken, _serverId, "org.silastandard/core/SiLAService/v1" ) ).Returns( -1 );
            _configProviderMock.Setup( p => p.AuthorizationProvider ).Returns( providerMock.Object );
            _configProviderMock.Setup(p => p.IsValidApiKey(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            Assert.Throws<AuthorizationInvalidAccessTokenException>((NUnit.Framework.TestDelegate)(() => _authService.Intercept( "org.silastandard/core/SiLAService/v1/Command/GetFeatureDefinition", accessToken ) ));
        }

        [Test]
        public void AuthenticateRequest_ApiKeyWorks()
        {
            var accessToken = Guid.NewGuid().ToString();
            var command = "org.silastandard/core/SiLAService/v1/Command/GetFeatureDefinition";

            _configProviderMock.Setup(p => p.IsValidApiKey(accessToken, command)).Returns(true);

            var interception = _authService.Intercept(command, accessToken);
            Assert.That(interception, Is.Null);
        }

        [TestCase( "test", "wrong password" )]
        [TestCase( "testadmin", "test-pwd" )]
        [TestCase( "doesnotexist", "no-pwd" )]
        public void Login_WrongCredentials_ThrowsException( string userName, string password )
        {
            Assert.Throws<AuthenticationFailedException>((NUnit.Framework.TestDelegate)(() => _authService.Login( userName, password, _serverId, new List<string>()
            {
                "org.silastandard/core/SiLAService/v1"
            } ) ));
        }

        [Test]
        public void Login_DifferentServer_ThrowsException()
        {
            _serverMock.Setup( s => s.ServerName ).Returns( "Test Server" );
            Assert.Throws<AuthenticationFailedException>((NUnit.Framework.TestDelegate)(() => _authService.Login( "testadmin", "testadmin-pwd", Guid.NewGuid().ToString(), new List<string>() ) ));
        }
    }
}
