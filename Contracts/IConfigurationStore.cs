using System;
using System.Collections.Generic;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes an interface to read configurations
    /// </summary>
    public interface IConfigurationStore
    {
        /// <summary>
        /// Gets the available configuration names for the provided file type
        /// </summary>
        /// <param name="configurationType">The type of the configuration</param>
        /// <returns>A collection of possible names of configuration</returns>
        IEnumerable<string> GetNames( Type configurationType );

        /// <summary>
        /// Determines if a configuration of the given type with the given name exists
        /// </summary>
        /// <param name="name">The name of the configuration</param>
        /// <param name="type">The type of the configuration</param>
        /// <returns>True, if the configuration entry exists, otherwise false</returns>
        bool Exists( string name, Type type );

        /// <summary>
        /// Deletes the configuration with the given name
        /// </summary>
        /// <param name="name">The name of the configuration</param>
        /// <param name="type">The type of the configuration</param>
        /// <returns>True, if the configuration entry existed, otherwise false</returns>
        bool Delete( string name, Type type );

        /// <summary>
        /// Reads a configuration of the given type
        /// </summary>
        /// <typeparam name="T">The type of the configuration</typeparam>
        /// <param name="name">The name of the configuration file</param>
        /// <returns>The deserialized configuration</returns>
        T Read<T>( string name );

        /// <summary>
        /// Writes the configuration with the given name
        /// </summary>
        /// <typeparam name="T">The data type of the configuration</typeparam>
        /// <param name="name">The name of the configuration file</param>
        /// <param name="value">The configuration to store</param>
        void Write<T>( string name, T value );
    }
}
