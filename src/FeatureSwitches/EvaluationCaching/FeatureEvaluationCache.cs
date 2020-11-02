using System;
using System.Collections.Concurrent;
using FeatureSwitches.Definitions;

namespace FeatureSwitches.EvaluationCaching
{
    public sealed class FeatureEvaluationCache : IFeatureEvaluationCache, IDisposable
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object?>> cache =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, object?>>();

        private readonly IFeatureDefinitionProvider featureDefinitionProvider;

        public FeatureEvaluationCache(IFeatureDefinitionProvider featureDefinitionProvider)
        {
            this.featureDefinitionProvider = featureDefinitionProvider;
            this.featureDefinitionProvider.Changed += this.FeatureDatabase_Changed;
        }

#pragma warning disable CA1021 // Avoid out parameters
        public bool TryGetValue<T>(string feature, string sessionContextValue, out T value)
#pragma warning restore CA1021 // Avoid out parameters
        {
            if (this.cache.TryGetValue(feature, out var dict))
            {
                if (dict.TryGetValue(sessionContextValue, out var obj))
                {
                    if (obj is T realObj)
                    {
                        value = realObj;
                        return true;
                    }
                    else
                    {
                        value = default!;
                        return false;
                    }
                }
            }

            value = default!;
            return false;
        }

        public void AddOrUpdate<T>(string feature, string sessionContextValue, T value)
        {
            var d = this.cache.GetOrAdd(feature, (x) =>
            {
                return new ConcurrentDictionary<string, object?>();
            });

            d[sessionContextValue] = value;
        }

        public void Dispose()
        {
            this.featureDefinitionProvider.Changed -= this.FeatureDatabase_Changed;
        }

        private void FeatureDatabase_Changed(object? sender, FeatureDefinitionChangeEventArgs e)
        {
            this.cache.TryRemove(e.Feature, out _);
        }
    }
}
