namespace FeatureSwitches;

public interface IFeatureService
{
    /// <summary>
    /// Gets all defined feature switches.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of defined featureswitch names.</returns>
    Task<string[]> GetFeatures(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests if the boolean featureswitch is on.
    /// </summary>
    /// <param name="feature">The featureswitch name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the switch is on, false if off or the feature doesn't exist.</returns>
    Task<bool> IsOn(string feature, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests if the boolean featureswitch is on within the specified evaluation context.
    /// The evaluation context is passed into featurefilter's that implement <see cref="Filters.IContextualFeatureFilter"/>.
    /// </summary>
    /// <param name="feature">The featureswitch name.</param>
    /// <param name="evaluationContext">The evaluationcontext.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <typeparam name="TEvaluationContext">The evaluation context type.</typeparam>
    /// <returns>True if the switch is on, false if off or the feature doesn't exist.</returns>
    Task<bool> IsOn<TEvaluationContext>(string feature, TEvaluationContext evaluationContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current value of featureswitch.
    /// If the defined featureswitch type doesn't match <typeparamref name="TFeatureType"/> a default is returned.
    /// </summary>
    /// <param name="feature">The featureswitch name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <typeparam name="TFeatureType">The feature type.</typeparam>
    /// <returns>The current switch value, or a default value if the feature doesn't exist.</returns>
    Task<TFeatureType?> GetValue<TFeatureType>(string feature, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current value of the featureswitch within the specified evaluation context.
    /// The evaluation context is passed into featurefilter's that implement <see cref="Filters.IContextualFeatureFilter"/>.
    /// If the defined featureswitch type doesn't match <typeparamref name="TFeatureType"/> a default is returned.
    /// </summary>
    /// <param name="feature">The featureswitch name.</param>
    /// <param name="evaluationContext">The evaluationcontext.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <typeparam name="TFeatureType">The feature type.</typeparam>
    /// <typeparam name="TEvaluationContext">The evaluation context type.</typeparam>
    /// <returns>The current switch value, or a default value if the feature doesn't exist.</returns>
    Task<TFeatureType?> GetValue<TFeatureType, TEvaluationContext>(string feature, TEvaluationContext evaluationContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current value of the featureswitch as a byte array.
    /// </summary>
    /// <param name="feature">The featureswitch name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The current switch value, or null if the feature doesn't exist.</returns>
    Task<byte[]?> GetBytes(string feature, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current value of the featureswitch within the specified evaluation context as a byte array.
    /// The evaluation context is passed into featurefilter's that implement <see cref="Filters.IContextualFeatureFilter"/>.
    /// </summary>
    /// <param name="feature">The featureswitch name.</param>
    /// <param name="evaluationContext">The evaluationcontext.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <typeparam name="TEvaluationContext">The evaluation context type.</typeparam>
    /// <returns>The current switch value, or null if the feature doesn't exist.</returns>
    Task<byte[]?> GetBytes<TEvaluationContext>(string feature, TEvaluationContext evaluationContext, CancellationToken cancellationToken = default);
}
