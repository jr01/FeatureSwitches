namespace FeatureSwitches.Definitions;

public sealed class FeatureFilterGroupDefinition
{
    /// <summary>
    /// Gets or sets the name of the group.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether the group is on.
    /// </summary>
    public bool IsOn { get; set; }

    /// <summary>
    /// Gets or sets the on value as UTF-8 encoded JSON.
    /// </summary>
    public object? OnValue { get; set; }
}
