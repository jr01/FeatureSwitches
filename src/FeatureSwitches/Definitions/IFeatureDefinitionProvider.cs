using System;
using System.Threading;
using System.Threading.Tasks;

namespace FeatureSwitches.Definitions
{
    /// <summary>
    /// Provides definitions for featureswitches.
    /// </summary>
    public interface IFeatureDefinitionProvider
    {
        /// <summary>
        /// Gets a list of featureswitches.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The names of all defined featureswitches.</returns>
        Task<string[]> GetFeatures(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the feature definition.
        /// </summary>
        /// <param name="feature">The featureswitch name.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The definition.</returns>
        Task<FeatureDefinition?> GetFeatureDefinition(string feature, CancellationToken cancellationToken = default);
    }
}
