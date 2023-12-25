using System.Text.Json.Serialization;

namespace FeatureSwitches.Filters;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ParallelChange
{
    /// <summary>
    /// Migrated.
    /// </summary>
    /// <remarks>
    /// The default(ParallelChange) == Migrated, so when setting is set to Migrated
    /// isOn("feature") without the ParallelChange context returns true.</remarks>
    Migrated,

    /// <summary>
    /// Expanded.
    /// </summary>
    Expanded,

    /// <summary>
    /// Contracted.
    /// </summary>
    Contracted,
}
