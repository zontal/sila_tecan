using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Recovery.Tests
{
    [TestFixture]
    public class RecoveryTests
    {
        private const string DefaultExecutionUuid = "execution uuid";
        private readonly IReadOnlyDictionary<string, string> _context = new Dictionary<string, string>
        {
            { nameof(IServerCommand.CommandExecutionUuid), DefaultExecutionUuid },
        };
        private readonly Exception _exception = new Exception( "Bloody exception" );

        [Test]
        public void Recover_UsesSelectedRecovery()
        {
            var strategy1 = CreateMockStrategy( "Strategy1", RecoveryResult.Retry );
            var strategy2 = CreateMockStrategy( "Strategy2", RecoveryResult.Continue );

            var server = new Mock<ISiLAServer>();
            var execution = new ObservableCommandExecution( DefaultExecutionUuid, "funny stuff", null, null );
            server.Setup( s => s.GetCommandExecution( DefaultExecutionUuid ) ).Returns( execution );

            var recoveryService = new ErrorRecoveryService( new[] { strategy1.Object, strategy2.Object }, server.Object );

            var recoverTask = recoveryService.Recover( _exception, _context, TimeSpan.FromMinutes( 1 ), 0 );
            Thread.Sleep( 1000 );
            Assert.That( recoverTask.IsCompleted, Is.False );

            var recoverableErrors = recoveryService.RecoverableErrors.ToList();
            Assert.That( recoverableErrors.Count, Is.EqualTo( 1 ) );
            Assert.That( recoverableErrors[0].CommandExecutionUUID, Is.EqualTo( DefaultExecutionUuid ) );
            Assert.That( recoverableErrors[0].ContinuationOptions.Count, Is.EqualTo( 2 ) );
            Assert.That( recoverableErrors[0].CommandIdentifier, Is.EqualTo( execution.CommandIdentifier ) );
            Assert.That( recoverableErrors[0].ErrorMessage, Is.EqualTo( _exception.Message ) );

            recoveryService.ExecuteContinuationOption( DefaultExecutionUuid, "Strategy1", null );
            recoverTask.Wait( 1000 );
            Assert.That( recoverTask.Result, Is.EqualTo( RecoveryResult.Retry ) );
            strategy1.Verify( s => s.Recover( _exception, _context, null ), Times.Once );
            strategy2.Verify( s => s.Recover( _exception, _context, null ), Times.Never );
        }

        [Test]
        public void Recover_AbortReturnsThrow()
        {
            var strategy1 = CreateMockStrategy( "Strategy1", RecoveryResult.Retry );
            var strategy2 = CreateMockStrategy( "Strategy2", RecoveryResult.Continue );

            var server = new Mock<ISiLAServer>();
            var execution = new ObservableCommandExecution( DefaultExecutionUuid, "funny stuff", null, null );
            server.Setup( s => s.GetCommandExecution( DefaultExecutionUuid ) ).Returns( execution );

            var recoveryService = new ErrorRecoveryService( new[] { strategy1.Object, strategy2.Object }, server.Object );

            var recoverTask = recoveryService.Recover( _exception, _context, TimeSpan.FromMinutes( 1 ), 0 );
            Thread.Sleep( 1000 );
            Assert.That( recoverTask.IsCompleted, Is.False );

            recoveryService.AbortErrorHandling( DefaultExecutionUuid );
            recoverTask.Wait( 1000 );
            Assert.That( recoverTask.Result, Is.EqualTo( RecoveryResult.Throw ) );
        }

        [Test]
        public void Recover_NonexistingContinuation_Throws()
        {
            var strategy1 = CreateMockStrategy( "Strategy1", RecoveryResult.Retry );
            var strategy2 = CreateMockStrategy( "Strategy2", RecoveryResult.Continue );

            var server = new Mock<ISiLAServer>();
            var execution = new ObservableCommandExecution( DefaultExecutionUuid, "funny stuff", null, null );
            server.Setup( s => s.GetCommandExecution( DefaultExecutionUuid ) ).Returns( execution );

            var recoveryService = new ErrorRecoveryService( new[] { strategy1.Object, strategy2.Object }, server.Object );

            var recoverTask = recoveryService.Recover( _exception, _context, TimeSpan.FromMinutes( 1 ), 0 );
            Thread.Sleep( 1000 );
            Assert.That( recoverTask.IsCompleted, Is.False );

            Assert.Throws<UnknownContinuationOptionException>((NUnit.Framework.TestDelegate)(() => recoveryService.ExecuteContinuationOption( DefaultExecutionUuid, "Does not exist", null ) ));
        }

        [Test]
        public void Recover_NonexistingExecutionUuid_Throws()
        {
            var strategy1 = CreateMockStrategy( "Strategy1", RecoveryResult.Retry );
            var strategy2 = CreateMockStrategy( "Strategy2", RecoveryResult.Continue );

            var server = new Mock<ISiLAServer>();
            var execution = new ObservableCommandExecution( DefaultExecutionUuid, "funny stuff", null, null );
            server.Setup( s => s.GetCommandExecution( DefaultExecutionUuid ) ).Returns( execution );

            var recoveryService = new ErrorRecoveryService( new[] { strategy1.Object, strategy2.Object }, server.Object );

            var recoverTask = recoveryService.Recover( _exception, _context, TimeSpan.FromMinutes( 1 ), 0 );
            Thread.Sleep( 1000 );
            Assert.That( recoverTask.IsCompleted, Is.False );

            Assert.Throws<InvalidCommandExecutionUUIDException>((NUnit.Framework.TestDelegate)(() => recoveryService.ExecuteContinuationOption( "does not exist", "Strategy1", null ) ));
        }

        [Test]
        public void Recover_UseDefaultStrategy()
        {
            var strategy1 = CreateMockStrategy( "Strategy1", RecoveryResult.Retry );
            var strategy2 = CreateMockStrategy( "Strategy2", RecoveryResult.Continue, priority: 1 );

            var server = new Mock<ISiLAServer>();
            var execution = new ObservableCommandExecution( DefaultExecutionUuid, "funny stuff", null, null );
            server.Setup( s => s.GetCommandExecution( DefaultExecutionUuid ) ).Returns( execution );

            var recoveryService = new ErrorRecoveryService( new[] { strategy1.Object, strategy2.Object }, server.Object );

            var recoverTask = recoveryService.Recover( _exception, _context, TimeSpan.FromSeconds( 2 ), 0 );
            Thread.Sleep( 1000 );
            Assert.That( recoverTask.IsCompleted, Is.False );

            var recoverableErrors = recoveryService.RecoverableErrors.ToList();
            Assert.That( recoverableErrors.Count, Is.EqualTo( 1 ) );
            Assert.That( recoverableErrors[0].CommandExecutionUUID, Is.EqualTo( DefaultExecutionUuid ) );
            Assert.That( recoverableErrors[0].ContinuationOptions.Count, Is.EqualTo( 2 ) );
            Assert.That( recoverableErrors[0].CommandIdentifier, Is.EqualTo( execution.CommandIdentifier ) );
            Assert.That( recoverableErrors[0].ErrorMessage, Is.EqualTo( _exception.Message ) );
            Assert.That( recoverableErrors[0].DefaultOption, Is.EqualTo( "Strategy2" ) );

            recoverTask.Wait( 2000 );
            Assert.That( recoverTask.Result, Is.EqualTo( RecoveryResult.Continue ) );
            strategy1.Verify( s => s.Recover( _exception, _context, null ), Times.Never );
            strategy2.Verify( s => s.Recover( _exception, _context, null ), Times.Once );
        }

        private Mock<IRecoveryStrategy> CreateMockStrategy( string name, RecoveryResult result, int priority = 0 )
        {
            var mockStrategy = new Mock<IRecoveryStrategy>();
            mockStrategy.Setup( s => s.Identifier ).Returns( name );
            mockStrategy.Setup( s => s.Description ).Returns( name );
            mockStrategy.Setup( s => s.ParameterType ).Returns( null as Type );
            mockStrategy.Setup( s => s.Priority ).Returns( priority );
            mockStrategy.Setup( s => s.CanRecover( _exception, _context, It.IsAny<int>() ) )
                .Returns( true );
            mockStrategy.Setup( s => s.Recover( _exception, _context, It.IsAny<object>() ) )
                .Returns( result );
            return mockStrategy;
        }
    }
}
