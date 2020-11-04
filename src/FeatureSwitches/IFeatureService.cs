using System.Threading.Tasks;

namespace FeatureSwitches
{
    public interface IFeatureService
    {
        /// <summary>
        /// Gets all defined feature switches.
        /// </summary>
        /// <returns>A list of defined featureswitch names.</returns>
        Task<string[]> GetFeatures();

        /// <summary>
        /// Tests if the boolean featureswitch is enabled.
        /// </summary>
        /// <param name="feature">The featureswitch name.</param>
        /// <returns>True if the switch is enabled, false if not enabled or the feature doesn't exist.</returns>
        Task<bool> IsEnabled(string feature);

        /// <summary>
        /// Tests if the boolean featureswitch is enabled within the specified evaluation context.
        /// The evaluation context is passed into featurefilter's that implement <see cref="Filters.IContextualFeatureFilter"/>.
        /// </summary>
        /// <param name="feature">The featureswitch name.</param>
        /// <param name="evaluationContext">The evaluationcontext.</param>
        /// <typeparam name="TEvaluationContext">The evaluation context type.</typeparam>
        /// <returns>True if the switch is enabled, false if not enabled or the feature doesn't exist.</returns>
        Task<bool> IsEnabled<TEvaluationContext>(string feature, TEvaluationContext evaluationContext);

        /// <summary>
        /// Gets the current value of featureswitch.
        /// If the defined featureswitch type doesn't match <typeparamref name="TFeatureType"/> a default is returned.
        /// </summary>
        /// <param name="feature">The featureswitch name.</param>
        /// <typeparam name="TFeatureType">The feature type.</typeparam>
        /// <returns>The current switch value, or a default value if the feature doesn't exist.</returns>
        Task<TFeatureType> GetValue<TFeatureType>(string feature);

        /// <summary>
        /// Gets the current value of the featureswitch within the specified evaluation context.
        /// The evaluation context is passed into featurefilter's that implement <see cref="Filters.IContextualFeatureFilter"/>.
        /// If the defined featureswitch type doesn't match <typeparamref name="TFeatureType"/> a default is returned.
        /// </summary>
        /// <param name="feature">The featureswitch name.</param>
        /// <param name="evaluationContext">The evaluationcontext.</param>
        /// <typeparam name="TFeatureType">The feature type.</typeparam>
        /// <typeparam name="TEvaluationContext">The evaluation context type.</typeparam>
        /// <returns>The current switch value, or a default value if the feature doesn't exist.</returns>
        Task<TFeatureType> GetValue<TFeatureType, TEvaluationContext>(string feature, TEvaluationContext evaluationContext);
    }
}