using System.Collections.Generic;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Extension of <see cref="IClientExecutionManager"/> to include queries which metadata are necessary
    /// </summary>
    public interface IClientExecutionManagerEx : IClientExecutionManager
    {
        /// <summary>
        /// Gets a collection of fully qualified metadata identifiers that are necessary to run the provided command
        /// </summary>
        /// <param name="commandIdentifier">The fully qualified identifier of either property or command</param>
        /// <returns>A collection of fully qualified metadata necessary</returns>
        IEnumerable<string> GetRequiredMetadata(string commandIdentifier);
    }
}
