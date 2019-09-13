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
        /// Creates a server definition for gRPC
        /// </summary>
        /// <returns>A gRPC server definition</returns>
        ServerServiceDefinition CreateServiceDefinition();
    }
}
