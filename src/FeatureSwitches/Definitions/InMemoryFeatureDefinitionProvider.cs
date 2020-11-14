﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FeatureSwitches.Definitions
{
    public class InMemoryFeatureDefinitionProvider : IFeatureDefinitionProvider
    {
        private readonly Dictionary<string, FeatureDefinition> featureSwitches = new ();

        public Task<string[]> GetFeatures(CancellationToken cancellationToken = default)
        {
            var features = this.featureSwitches.Keys.ToArray();
            return Task.FromResult(features);
        }

        public Task<FeatureDefinition?> GetFeatureDefinition(string feature, CancellationToken cancellationToken = default)
        {
            this.featureSwitches.TryGetValue(feature, out var definition);
            return Task.FromResult<FeatureDefinition?>(definition);
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

            var filter = definition.Filters.FirstOrDefault(x => x.Type == featureFilterName && x.Group == group);
            if (filter is null)
            {
                filter = new FeatureFilterDefinition
                {
                    Type = featureFilterName,
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
                filter.Config = Encoding.UTF8.GetBytes(stringConfig);
            }
            else
            {
                filter.Config = JsonSerializer.SerializeToUtf8Bytes(config);
            }
        }

        /// <summary>
        /// Adds a definition for a <see cref="bool"/> feature.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="isOn">If the feature should be on or off, on by default.</param>
        public void SetFeature(string feature, bool isOn = true) =>
            this.SetFeature<bool>(feature, isOn: isOn, offValue: false, onValue: true);

        /// <summary>
        /// Adds a definition for a typed feature.
        /// </summary>
        /// <typeparam name="TFeatureType">The feature type.</typeparam>
        /// <param name="feature">The feature.</param>
        /// <param name="isOn">If the feature should be on or off, on by default.</param>
        /// <param name="offValue">The value to return when the feature is off.</param>
        /// <param name="onValue">The value to return when the feature is on and no feature groups have been defined.</param>
        public void SetFeature<TFeatureType>(string feature, bool isOn = true, TFeatureType offValue = default, TFeatureType onValue = default)
        {
            if (!this.featureSwitches.TryGetValue(feature, out var definition))
            {
                definition = new ();
                this.featureSwitches.Add(feature, definition);
            }

            definition.OffValue = JsonSerializer.SerializeToUtf8Bytes(offValue);
            definition.OnValue = JsonSerializer.SerializeToUtf8Bytes(onValue);
            definition.IsOn = isOn;
        }

        /// <summary>
        /// Defines a feature filter group for a <see cref="bool"/> feature.
        /// </summary>
        /// <param name="feature">The feature name.</param>
        /// <param name="group">The group name.</param>
        /// <param name="isOn">If the feature filter group should be on or off, on by default.</param>
        public void SetFeatureGroup(string feature, string group, bool isOn = true) =>
            this.SetFeatureGroup<bool>(feature, group, isOn: isOn, onValue: true);

        /// <summary>
        /// Defines a feature filter group for a typed feature.
        /// </summary>
        /// <typeparam name="TFeatureType">The feature type.</typeparam>
        /// <param name="feature">The feature.</param>
        /// <param name="group">The feature filter group.</param>
        /// <param name="isOn">If the feature filter group should be on or off, on by default.</param>
        /// <param name="onValue">The value to return when the feature is on and the group matches.</param>
        public void SetFeatureGroup<TFeatureType>(string feature, string group, bool isOn = true, TFeatureType onValue = default)
        {
            if (!this.featureSwitches.TryGetValue(feature, out var definition))
            {
                throw new InvalidOperationException($"Feature {feature} must be defined first.");
            }

            var featureFilterGroup = definition.FilterGroups.FirstOrDefault(x => x.Name == group);
            if (featureFilterGroup is null)
            {
                featureFilterGroup = new ()
                {
                    Name = group
                };
                definition.FilterGroups.Add(featureFilterGroup);
            }

            featureFilterGroup.OnValue = JsonSerializer.SerializeToUtf8Bytes(onValue);
            featureFilterGroup.IsOn = isOn;
        }
    }
}
