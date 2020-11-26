namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes an interface to read configurations
    /// </summary>
    public interface IConfigurationStore
    {
        /// <summary>
        /// Reads a configuration of the given type
        /// </summary>
        /// <typeparam name="T">The type of the configuration</typeparam>
        /// <param name="name">The name of the configuration file</param>
        /// <returns>The deserialized configuration</returns>
        T Read<T>( string name );
    }
}
