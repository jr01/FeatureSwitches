namespace FeatureSwitches.Definitions;

public class FeatureFilterDefinition
{
    /// <summary>
    /// Gets or sets the filter name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the filter settings.
    /// </summary>
    public object? Settings { get; set; }

    /// <summary>
    /// Gets or sets the filter group.
    /// </summary>
    public string? Group { get; set; }
}
