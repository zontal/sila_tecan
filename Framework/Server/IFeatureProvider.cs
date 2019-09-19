using System.Reflection;
using Grpc.Core;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes an interface for a class that provides a feature
    /// </summary>
    public interface IFeatureProvider
    {
        /// <summary>
        /// Gets the feature provided by this instance
        /// </summary>
        Feature FeatureDefinition { get; }

        /// <summary>
        /// Gets the command with the given identifier
        /// </summary>
        /// <param name="commandIdentifier">A fully qualified command identifier</param>
        /// <returns>A method object or null, if the command is not supported</returns>
        MethodInfo GetCommand( string commandIdentifier );

        /// <summary>
        /// Gets the property with the given identifier
        /// </summary>
        /// <param name="propertyIdentifier">A fully qualified property identifier</param>
        /// <returns>A property object or null, if the property is not supported</returns>
        PropertyInfo GetProperty( string propertyIdentifier );

        /// <summary>
        /// Creates a server definition for gRPC
        /// </summary>
        /// <returns>A gRPC server definition</returns>
        ServerServiceDefinition CreateServiceDefinition();
    }
}
