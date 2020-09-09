using System;
using System.IO;
using Tecan.Sila2;

namespace Contracts
{
    /// <summary>
    /// This is an example interfae for a service to greet people
    /// </summary>
    [SilaFeature]
    public interface IGreetingService
    {
        /// <summary>
        /// Says hello to the provided name
        /// </summary>
        /// <param name="name">The name of the person greeted</param>
        /// <exception cref="UnfriendlyNameException">Thrown if the name contains digits</exception>
        /// <returns>A greeting</returns>
        string SayHello( [MaximalLength(20)] string name);

        /// <summary>
        /// Echos a custom data structure
        /// </summary>
        /// <param name="structure">the data structure to echo</param>
        /// <returns>the echoed data structure</returns>
        DemoStructure EchoStructure(DemoStructure structure);

        /// <summary>
        /// Simply echoes the stream that is passed into it
        /// </summary>
        /// <param name="stream">the stream to echo</param>
        /// <returns>the echoed stream</returns>
        Stream EchoStream(Stream stream);
    }

    /// <summary>
    /// This structure is just to demo creating structured objects
    /// </summary>
    public struct DemoStructure
    {
        public DemoStructure(long x, long y) : this()
        {
            X = x;
            Y = y;
        }

        public long X { get; }
        public long Y { get; }
    }

    /// <summary>
    /// Thrown if the name contains digits
    /// </summary>
    public class UnfriendlyNameException : Exception
    {
        public UnfriendlyNameException(string message) : base(message)
        {

        }
    }
}
