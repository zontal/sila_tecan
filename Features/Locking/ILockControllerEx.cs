using Tecan.Sila2.Server;

namespace Tecan.Sila2.Locking
{
    /// <summary>
    /// Denotes an interface for a lock controller
    /// </summary>
    public partial interface ILockController
    {
        /// <summary>
        /// Gets the request interceptor for the lock identifier
        /// </summary>
        [MetadataType(typeof(string))]
        IRequestInterceptor LockIdentifier { get; }
    }
}
