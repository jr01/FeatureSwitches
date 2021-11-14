namespace FeatureSwitches.Definitions;

public class FeatureDefinition
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
    /// Gets or sets a list of filters.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<FeatureFilterDefinition> Filters { get; set; } = new List<FeatureFilterDefinition>();
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Gets or sets a list of filter groups.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<FeatureFilterGroupDefinition> FilterGroups { get; set; } = new List<FeatureFilterGroupDefinition>();
#pragma warning restore CA2227 // Collection properties should be read only
}
