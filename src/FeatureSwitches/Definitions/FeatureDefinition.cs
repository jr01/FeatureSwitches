namespace FeatureSwitches.Definitions;

public sealed class FeatureDefinition
{
    /// <summary>
    /// Gets or sets the feature name.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether the feature is on.
    /// </summary>
    public bool IsOn { get; set; }

    /// <summary>
    /// Gets or sets the off value.
    /// </summary>
    public object? OffValue { get; set; }

    /// <summary>
    /// Gets or sets the on value.
    /// </summary>
    public object? OnValue { get; set; }

    /// <summary>
    /// Gets a list of filters.
    /// </summary>
    public IList<FeatureFilterDefinition> Filters { get; init; } = [];

    /// <summary>
    /// Gets a list of filter groups.
    /// </summary>
    public IList<FeatureFilterGroupDefinition> FilterGroups { get; init; } = [];
}
