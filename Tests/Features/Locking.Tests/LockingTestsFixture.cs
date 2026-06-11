using NUnit.Framework;

namespace Tecan.Sila2.Locking.Tests
{
    [TestFixture]
    public class LockingTestsFixture
    {
        private LockControllerService _lockController;

        [SetUp]
        public void Initialize()
        {
            _lockController = new LockControllerService();
        }

        [Test]
        public void Intercept_NoLockIdentifier_ThrowsException()
        {
            Assert.Throws<InvalidLockIdentifierException>((NUnit.Framework.TestDelegate)(() => _lockController.Intercept( "org.silastandard/core/SiLAService/v1/Command/GetFeatureDefinition", null )));
        }

        [Test]
        public void Intercept_LockedServer_Succeeds()
        {
            var lockIdentifier = "Foo";

            _lockController.LockServer( lockIdentifier, 3600 );
            Assert.That(_lockController.IsLocked, Is.True);

            var interception = _lockController.Intercept( "org.silastandard/core/SiLAService/v1/Command/GetFeatureDefinition", lockIdentifier );
            Assert.That(interception, Is.Null);
            Assert.DoesNotThrow((NUnit.Framework.TestDelegate)(() => _lockController.UnlockServer( lockIdentifier )));
        }

        [Test]
        public void Intercept_LockedServerWrongIdentifier_ThrowsException()
        {
            var lockIdentifier = "Foo";

            _lockController.LockServer( lockIdentifier, 3600 );
            Assert.That( _lockController.IsLocked, Is.True );

            Assert.Throws<InvalidLockIdentifierException>((NUnit.Framework.TestDelegate)(() => _lockController.Intercept( "org.silastandard/core/SiLAService/v1/Command/GetFeatureDefinition", "Bar" ) ));

            var interception = _lockController.Intercept( "org.silastandard/core/SiLAService/v1/Command/GetFeatureDefinition", lockIdentifier );
            Assert.That( interception, Is.Null );
        }

        [Test]
        public void UnlockServer_WrongIdentifier_ThrowsException()
        {
            var lockIdentifier = "Foo";

            _lockController.LockServer( lockIdentifier, 3600 );
            Assert.That( _lockController.IsLocked, Is.True );

            Assert.Throws<ServerAlreadyLockedException>((NUnit.Framework.TestDelegate)(() => _lockController.UnlockServer("Bar")));
        }

        [Test]
        public void UnlockServer_Unlocked_ThrowsException()
        {
            Assert.Throws<ServerNotLockedException>((NUnit.Framework.TestDelegate)(() => _lockController.UnlockServer( "Foo" ) ));
        }

        [Test]
        public void LockServer_AlreadyLocked_ThrowsException()
        {
            var lockIdentifier = "Foo";

            _lockController.LockServer( lockIdentifier, 3600 );
            Assert.That( _lockController.IsLocked, Is.True );

            Assert.Throws<ServerAlreadyLockedException>((NUnit.Framework.TestDelegate)(() => _lockController.LockServer( "Bar", 3600 ) ));
        }
    }
}
