using System.Text.Json;

namespace FeatureSwitches.Definitions;

public sealed class InMemoryFeatureDefinitionProvider : IFeatureDefinitionProvider
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };
    private readonly Dictionary<string, FeatureDefinition> featureSwitches = [];

    public Task<string[]> GetFeatures(CancellationToken cancellationToken = default)
    {
        var features = this.featureSwitches.Keys.ToArray();
        return Task.FromResult(features);
    }

    public Task<FeatureDefinition?> GetFeatureDefinition(string feature, CancellationToken cancellationToken = default)
    {
        this.featureSwitches.TryGetValue(feature, out var definition);
        return Task.FromResult(definition);
    }

    /// <summary>
    /// Apply the feature filter to the feature.
    /// </summary>
    /// <param name="feature">The feature.</param>
    /// <param name="featureFilterName">The feature filter name.</param>
    /// <param name="config">Optional feature filter configuration, either a JSON string, or an object that can be converted to JSON.</param>
    /// <param name="group">An optional feature filter group.</param>
    public void SetFeatureFilter(string feature, string featureFilterName, object? config = null, string? group = null)
    {
        if (!this.featureSwitches.TryGetValue(feature, out var definition))
        {
            throw new InvalidOperationException($"Feature {feature} must be defined first.");
        }

        var filter = definition.Filters.FirstOrDefault(x => x.Name == featureFilterName && x.Group == group);
        if (filter is null)
        {
            filter = new FeatureFilterDefinition
            {
                Name = featureFilterName,
            };

            definition.Filters.Add(filter);
        }

        filter.Group = group;
        if (filter.Group is not null)
        {
            if (!definition.FilterGroups.Any(g => g.Name == group))
            {
                throw new InvalidOperationException($"Feature group {group} must be defined first for feature {feature}");
            }
        }

        if (config is string stringConfig)
        {
            filter.Settings = JsonSerializer.Deserialize<object?>(stringConfig);
        }
        else
        {
            filter.Settings = config;
        }
    }

    /// <summary>
    /// Adds a new definition for a <see cref="bool"/> feature.
    /// </summary>
    /// <param name="feature">The feature.</param>
    /// <param name="isOn">If the feature should be on or off, on by default.</param>
    public void SetFeature(string feature, bool isOn = true)
    {
        this.SetFeature(feature, isOn: isOn, offValue: false, onValue: true);
    }

    /// <summary>
    /// Adds a new definition for a typed feature.
    /// </summary>
    /// <typeparam name="TFeatureType">The feature type.</typeparam>
    /// <param name="feature">The feature.</param>
    /// <param name="isOn">If the feature should be on or off, on by default.</param>
    /// <param name="offValue">The value to return when the feature is off.</param>
    /// <param name="onValue">The value to return when the feature is on and no feature groups have been defined.</param>
    public void SetFeature<TFeatureType>(string feature, bool isOn = true, TFeatureType? offValue = default, TFeatureType? onValue = default)
    {
        this.featureSwitches[feature] = new()
        {
            Name = feature,
            OffValue = offValue,
            OnValue = onValue,
            IsOn = isOn,
        };
    }

    /// <summary>
    /// Toggles a feature on/off.
    /// </summary>
    /// <param name="feature">The feature.</param>
    /// <param name="isOn">If the feature should be on or off.</param>
    public void ToggleFeature(string feature, bool isOn)
    {
        if (!this.featureSwitches.TryGetValue(feature, out var definition))
        {
            throw new InvalidOperationException($"Feature {feature} must be defined first.");
        }

        definition.IsOn = isOn;
    }

    /// <summary>
    /// Defines a new feature filter group for a <see cref="bool"/> feature.
    /// </summary>
    /// <param name="feature">The feature name.</param>
    /// <param name="group">The group name.</param>
    /// <param name="isOn">If the feature filter group should be on or off, on by default.</param>
    public void SetFeatureGroup(string feature, string group, bool isOn = true)
    {
        this.SetFeatureGroup(feature, group, isOn: isOn, onValue: true);
    }

    /// <summary>
    /// Defines a new feature filter group for a typed feature.
    /// </summary>
    /// <typeparam name="TFeatureType">The feature type.</typeparam>
    /// <param name="feature">The feature.</param>
    /// <param name="group">The feature filter group.</param>
    /// <param name="isOn">If the feature filter group should be on or off, on by default.</param>
    /// <param name="onValue">The value to return when the feature is on and the group matches.</param>
    public void SetFeatureGroup<TFeatureType>(string feature, string group, bool isOn = true, TFeatureType? onValue = default)
    {
        if (!this.featureSwitches.TryGetValue(feature, out var definition))
        {
            throw new InvalidOperationException($"Feature {feature} must be defined first.");
        }

        var featureFilterGroup = definition.FilterGroups.FirstOrDefault(x => x.Name == group);
        if (featureFilterGroup is null)
        {
            featureFilterGroup = new()
            {
                Name = group,
            };
            definition.FilterGroups.Add(featureFilterGroup);
        }

        featureFilterGroup.OnValue = onValue;
        featureFilterGroup.IsOn = isOn;
    }

    /// <summary>
    /// Toggles a featuregroup on/off.
    /// </summary>
    /// <param name="feature">The feature.</param>
    /// <param name="group">The feature group.</param>
    /// <param name="isOn">If the feature group should be on or off.</param>
    public void ToggleFeatureGroup(string feature, string group, bool isOn)
    {
        if (!this.featureSwitches.TryGetValue(feature, out var definition))
        {
            throw new InvalidOperationException($"Feature {feature} must be defined first.");
        }

        var featureFilterGroup = definition.FilterGroups.FirstOrDefault(x => x.Name == group) ??
            throw new InvalidOperationException($"Feature group {group} must be defined first.");
        featureFilterGroup.IsOn = isOn;
    }

    /// <summary>
    /// Load the features.
    /// </summary>
    /// <param name="features">The features.</param>
    public void Load(IEnumerable<FeatureDefinition> features)
    {
        ArgumentNullException.ThrowIfNull(features);

        this.featureSwitches.Clear();
        foreach (var feature in features)
        {
            this.featureSwitches.Add(feature.Name, feature);
        }
    }

    /// <summary>
    /// Load the features from a JSON formatted string.
    /// </summary>
    /// <remarks>
    /// The JSON should be structured according to <see cref="SaveToJson"/>.
    /// It can contain extra custom fields for enriching the feature definition, they are ignored.
    /// </remarks>
    /// <param name="json">The json.</param>
    public void LoadFromJson(string json)
    {
        var features = JsonSerializer.Deserialize<IEnumerable<FeatureDefinition>>(json)
            ?? throw new InvalidOperationException("Invalid json.");
        this.Load(features);
    }

    /// <summary>
    /// Save the features.
    /// </summary>
    /// <returns>A list of feature definitions.</returns>
    public IList<FeatureDefinition> Save()
    {
        return [.. this.featureSwitches.Values];
    }

    /// <summary>
    /// Save the feature definitions into pretty JSON format.
    /// </summary>
    /// <returns>A JSON formatted string.</returns>
    public string SaveToJson()
    {
        return JsonSerializer.Serialize(this.Save(), JsonSerializerOptions);
    }
}
