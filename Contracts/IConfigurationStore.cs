using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes an interface to read configurations
    /// </summary>
    public interface IConfigurationStore
    {
        /// <summary>
        /// Determines if a configuration of the given type with the given name exists
        /// </summary>
        /// <param name="name">The name of the configuration</param>
        /// <param name="type">The type of the configuration</param>
        /// <returns>True, if the configuration entry exists, otherwise false</returns>
        bool Exists( string name, Type type );

        /// <summary>
        /// Reads a configuration of the given type
        /// </summary>
        /// <typeparam name="T">The type of the configuration</typeparam>
        /// <param name="name">The name of the configuration file</param>
        /// <returns>The deserialized configuration</returns>
        T Read<T>( string name );
    }
}
