using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
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

        public Task<EvaluationCacheResult<T>?> GetItem<T>(string feature, string sessionContextValue)
        {
            if (this.cache.TryGetValue(feature, out var dict))
            {
                if (dict.TryGetValue(sessionContextValue, out var obj))
                {
                    if (obj is T realObj)
                    {
                        var item = new EvaluationCacheResult<T> { };
                        item.Result = realObj;
                        return Task.FromResult<EvaluationCacheResult<T>?>(item);
                    }
                }
            }

            return Task.FromResult<EvaluationCacheResult<T>?>(null);
        }

        public Task SetItem<T>(string feature, string sessionContextValue, T value)
        {
            var d = this.cache.GetOrAdd(feature, (x) =>
            {
                return new ConcurrentDictionary<string, object?>();
            });

            d[sessionContextValue] = value;

            return Task.CompletedTask;
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
