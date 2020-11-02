using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using FeatureSwitches.Filters;

namespace FeatureSwitches.Definitions
{
    public class InMemoryFeatureDefinitionProvider : IFeatureDefinitionProvider
    {
        private readonly Dictionary<string, byte[]> featureSwitches =
            new Dictionary<string, byte[]>();

        private readonly Dictionary<string, byte[]> featureFilterGroups =
            new Dictionary<string, byte[]>();

        private readonly Dictionary<string, List<InMemoryFeatureFilter>> featureFilters =
            new Dictionary<string, List<InMemoryFeatureFilter>>();

        private readonly ConcurrentDictionary<string, FeatureDefinition?> filterDefinitionCache =
            new ConcurrentDictionary<string, FeatureDefinition?>();

        public event EventHandler<FeatureDefinitionChangeEventArgs>? Changed;

        public string[] GetFeatures()
        {
            return this.featureSwitches.Keys.ToArray();
        }

        public FeatureDefinition? GetFeatureDefinition(string feature)
        {
            return this.filterDefinitionCache.GetOrAdd(feature, x =>
            {
                return this.GetFeatureDefinitionInternal(feature);
            });
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
            if (!this.featureFilters.TryGetValue(feature, out var filters))
            {
                filters = new List<InMemoryFeatureFilter>();
                this.featureFilters.Add(feature, filters);
            }

            var filter = filters.FirstOrDefault(x => x.Type == featureFilterName && x.Group == group);
            if (filter == null)
            {
                filter = new InMemoryFeatureFilter { Type = featureFilterName, Group = group };
                filters.Add(filter);
            }

            if (config is string stringConfig)
            {
                filter.Config = Encoding.UTF8.GetBytes(stringConfig);
            }
            else
            {
                filter.Config = JsonSerializer.SerializeToUtf8Bytes(config);
            }

            this.Reload(feature);
        }

        /// <summary>
        /// Adds a definition for a <see cref="bool"/> feature.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="isOn">If the feature should be On.</param>
        public void SetFeature(string feature, bool isOn = false) =>
            this.SetFeature<bool>(feature, isOn: isOn, offValue: false, onValue: true);

        /// <summary>
        /// Adds a definition for a typed feature.
        /// </summary>
        /// <typeparam name="TFeatureType">The feature type.</typeparam>
        /// <param name="feature">The feature.</param>
        /// <param name="isOn">If the feature should be On.</param>
        /// <param name="offValue">The value to return when the feature is off.</param>
        /// <param name="onValue">The value to return when the feature is on and no feature groups have been defined.</param>
        public void SetFeature<TFeatureType>(string feature, bool isOn = false, TFeatureType offValue = default, TFeatureType onValue = default)
        {
            this.featureSwitches[feature] = JsonSerializer.SerializeToUtf8Bytes(offValue);

            // Add the null group with the onValue and add an OnOff featurefilter as the first feature filter.
            this.SetFeatureGroup(feature, group: null, onValue: onValue);
            this.SetFeatureFilter(feature, "OnOff", config: new ScalarValueSetting<bool>(isOn), group: null);
        }

        /// <summary>
        /// Defines a feature filter group.
        /// </summary>
        /// <typeparam name="TFeatureType">The feature type.</typeparam>
        /// <param name="feature">The feature name.</param>
        /// <param name="onValue">The value to return when the feature is on and the group matches.</param>
        public void SetFeatureGroup<TFeatureType>(string feature, TFeatureType onValue) =>
            this.SetFeatureGroup(feature, null, onValue);

        /// <summary>
        /// Defines a feature filter group for a <see cref="bool"/> feature.
        /// </summary>
        /// <param name="feature">The feature name.</param>
        /// <param name="group">The group name.</param>
        public void SetFeatureGroup(string feature, string? group = null) =>
            this.SetFeatureGroup<bool>(feature, group, true);

        /// <summary>
        /// Defines a feature filter group for a typed feature.
        /// </summary>
        /// <typeparam name="TFeatureType">The feature type.</typeparam>
        /// <param name="feature">The feature.</param>
        /// <param name="group">The feature filter group.</param>
        /// <param name="onValue">The value to return when the feature is on and the group matches.</param>
        public void SetFeatureGroup<TFeatureType>(string feature, string? group = null, TFeatureType onValue = default)
        {
            this.featureFilterGroups[feature + "." + (group ?? string.Empty)] = JsonSerializer.SerializeToUtf8Bytes(onValue);

            this.Reload(feature);
        }

        private FeatureDefinition? GetFeatureDefinitionInternal(string feature)
        {
            var definition = new FeatureDefinition { };
            if (this.featureSwitches.TryGetValue(feature, out var switchValue))
            {
                definition.OffValue = switchValue;
            }
            else
            {
                return null;
            }

            if (this.featureFilters.TryGetValue(feature, out var filters))
            {
                foreach (var filter in filters)
                {
                    var filterGroup = new FeatureFilterGroupDefinition
                    {
                        Name = filter.Group,
                        OnValue = Array.Empty<byte>()
                    };
                    if (this.featureFilterGroups.TryGetValue(feature + "." + (filter.Group ?? string.Empty), out var filterGroupValue))
                    {
                        filterGroup.OnValue = filterGroupValue;
                    }

                    definition.FeatureFilters.Add(new FeatureFilterDefinition
                    {
                        Type = filter.Type,
                        Config = filter.Config,
                        Group = filterGroup
                    });
                }
            }

            return definition;
        }

        private void Reload(string feature)
        {
            this.filterDefinitionCache.TryRemove(feature, out _);

            this.Changed?.Invoke(this, new FeatureDefinitionChangeEventArgs(feature));
        }

        private class InMemoryFeatureFilter
        {
            public string Type { get; set; } = string.Empty;

            public byte[] Config { get; set; } = null!;

            public string? Group { get; set; }
        }
    }
}
