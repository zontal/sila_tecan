namespace Tecan.Sila2.Client
{
    /// <summary>
    /// This interface denotes a component that manages execution for multiple servers
    /// </summary>
    public interface IExecutionManagerFactory
    {
        /// <summary>
        /// Gets or creates an execution manager specific for the given server
        /// </summary>
        /// <param name="server">The server that should be connected to</param>
        /// <returns>An execution manager that handles calls to the given server</returns>
        IClientExecutionManager CreateExecutionManager( ServerData server );
    }
}
