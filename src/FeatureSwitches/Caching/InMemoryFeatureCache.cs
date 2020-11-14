using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FeatureSwitches.Caching
{
    public sealed class InMemoryFeatureCache : IFeatureCache
    {
        private readonly ConcurrentDictionary<string, CacheValue> cache = new ();
        private readonly Func<DateTimeOffset> timeResolver;

        public InMemoryFeatureCache()
            : this(() => DateTimeOffset.UtcNow)
        {
        }

        public InMemoryFeatureCache(Func<DateTimeOffset> timeResolver)
        {
            this.timeResolver = timeResolver;
        }

        public Task<byte[]?> GetItem(string feature, string context, CancellationToken cancellationToken = default)
        {
            if (this.cache.TryGetValue($"{feature}:{context}", out var cacheValue))
            {
                if (!cacheValue.AbsoluteExpiration.HasValue ||
                    cacheValue.AbsoluteExpiration > this.timeResolver())
                {
                    return Task.FromResult<byte[]?>(cacheValue.Value);
                }
            }

            return Task.FromResult<byte[]?>(null);
        }

        public Task SetItem(string feature, string context, byte[] value, FeatureCacheOptions? options = null, CancellationToken cancellationToken = default)
        {
            this.cache[$"{feature}:{context}"] = new CacheValue
            {
                Value = value
            };

            return Task.CompletedTask;
        }

        public Task Remove(string feature, CancellationToken cancellationToken = default)
        {
            var prefix = $"{feature}:";
            foreach (var item in this.cache.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                this.cache.TryRemove(item.Key, out _);
            }

            return Task.CompletedTask;
        }

        private class CacheValue
        {
            public byte[] Value { get; set; } = default!;

            public DateTimeOffset? AbsoluteExpiration { get; set; }
        }
    }
}
