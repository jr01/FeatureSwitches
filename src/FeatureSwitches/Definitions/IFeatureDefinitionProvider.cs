using System;

namespace FeatureSwitches.Definitions
{
    /// <summary>
    /// Provides definitions for featureswitches.
    /// </summary>
    public interface IFeatureDefinitionProvider
    {
        /// <summary>
        /// A featureswitch definition has changed.
        /// </summary>
        event EventHandler<FeatureDefinitionChangeEventArgs> Changed;

        /// <summary>
        /// Gets a list of featureswitches.
        /// </summary>
        /// <returns>The names of all defined featureswitches.</returns>
        string[] GetFeatures();

        /// <summary>
        /// Gets the feature definition.
        /// </summary>
        /// <param name="feature">The featureswitch name.</param>
        /// <returns>The definition.</returns>
        FeatureDefinition? GetFeatureDefinition(string feature);
    }
}
