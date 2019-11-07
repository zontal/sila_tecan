using System;
using System.Collections.Generic;
using System.IO;

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

        ICollection<string> PassCollection( ICollection<string> strings );

        SomeStructure PassStructure( SomeStructure structure );

        IObservableCommand<int> RunObservable();

        IIntermediateObservableCommand<int> RunIntermediateObservableCommand();
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
