using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tecan.Sila2.DynamicClient;

namespace Tecan.Sila2.IntegrationTests
{
    [SilaFeature]
    public interface ITestServer
    {
        bool PassBool( bool data );

        double PassDouble( double data );

        long PassLong( long data );

        int PassInt( int data );

        DateTimeOffset PassTimestamp( DateTimeOffset timestamp );

        TimeSpan PassTime( TimeSpan time );

        string PassString( string value );

        Stream PassStream( Stream value );

        FileInfo PassFile( FileInfo file );

        byte[] PassBytes( byte[] bytes );

        DynamicObjectProperty PassDynamic( DynamicObjectProperty dynamic );

        ICollection<string> PassCollection( ICollection<string> strings );

        SomeStructure PassStructure( SomeStructure structure );

        [Observable]
        IObservableCommand<int> RunObservable();

        [Observable]
        IIntermediateObservableCommand<int> RunIntermediateObservableCommand();

        [Observable]
        [Throws(typeof(ShouldHaveBeenCancelledException))]
        Task WaitForCancelOrThrow( TimeSpan duration, CancellationToken cancellationToken );

        [Throws(typeof(FakeException))]
        void RunIntoFailure();

        [Observable]
        [Throws(typeof(FakeException))]
        void RunIntoFailureObservably(CancellationToken cancellationToken);

        bool IsCancelled( string commandId );

        void ThrowRuntimeException();

        [Observable]
        int DummyValue
        {
            get;
        }

        void SetDummy( int value );
    }

    public class FakeException : Exception
    {
        public FakeException(string message) : base(message) { }
    }

    public class ShouldHaveBeenCancelledException : Exception
    {
        public ShouldHaveBeenCancelledException(string message) : base(message) { }
    }

    public class SomeStructure
    {
        public SomeStructure( bool boolean, double real, long @long, int @int, DateTimeOffset timestamp, TimeSpan time )
        {
            Boolean = boolean;
            Real = real;
            Long = @long;
            Int = @int;
            Timestamp = timestamp;
            Time = time;
        }

        public bool Boolean
        {
            get;
        }

        public double Real
        {
            get;
        }

        public long Long
        {
            get;
        }

        public int Int
        {
            get;
        }

        public DateTimeOffset Timestamp
        {
            get;
        }

        public TimeSpan Time
        {
            get;
        }
    }
}
