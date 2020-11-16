using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FeatureSwitches.Caching;
using FeatureSwitches.Definitions;
using FeatureSwitches.Filters;

namespace FeatureSwitches
{
    /// <summary>
    /// A featureservice implementation. Typically has as scoped lifetime.
    /// </summary>
    public class FeatureService : IFeatureService
    {
        private readonly IFeatureDefinitionProvider featureDefinitionProvider;
        private readonly IEnumerable<IFeatureCache> featureEvaluationCaches;
        private readonly IEnumerable<IFeatureFilterMetadata> filters;
        private readonly IFeatureCacheContextAccessor featureContextProvider;
        private readonly ConcurrentDictionary<string, IFeatureFilterMetadata> featureFilterMetadataCache = new ();

        public FeatureService(
            IFeatureDefinitionProvider featureDefinitionProvider,
            IEnumerable<IFeatureCache> featureEvaluationCaches,
            IFeatureCacheContextAccessor featureContextProvider,
            IEnumerable<IFeatureFilterMetadata> filters)
        {
            this.featureDefinitionProvider = featureDefinitionProvider;
            this.featureEvaluationCaches = featureEvaluationCaches;
            this.filters = filters;
            this.featureContextProvider = featureContextProvider;
        }

        public Task<bool> IsOn(string feature, CancellationToken cancellationToken = default) =>
            this.GetValue<bool>(feature, cancellationToken);

        public Task<bool> IsOn<TEvaluationContext>(string feature, TEvaluationContext? evaluationContext, CancellationToken cancellationToken = default) =>
            this.GetValue<bool, TEvaluationContext>(feature, evaluationContext, cancellationToken);

        public Task<TFeatureType?> GetValue<TFeatureType>(string feature, CancellationToken cancellationToken = default) =>
            this.GetValue<TFeatureType, object>(feature, null, cancellationToken);

        public async Task<TFeatureType?> GetValue<TFeatureType, TEvaluationContext>(
            string feature,
            TEvaluationContext? evaluationContext,
            CancellationToken cancellationToken = default)
        {
            var bytes = await this.GetBytes(feature, evaluationContext, cancellationToken).ConfigureAwait(false);
            if (bytes is null)
            {
                return default!;
            }

            return JsonSerializer.Deserialize<TFeatureType?>(bytes);
        }

        public Task<byte[]?> GetBytes(string feature, CancellationToken cancellationToken = default) =>
            this.GetBytes<object>(feature, null!, cancellationToken);

        public async Task<byte[]?> GetBytes<TEvaluationContext>(
            string feature,
            TEvaluationContext evaluationContext,
            CancellationToken cancellationToken = default)
        {
            var cacheContext = this.GetCacheContext(evaluationContext);
            foreach (var cache in this.featureEvaluationCaches)
            {
                var evalutionCachedResult = await cache.GetItem(feature, cacheContext, cancellationToken).ConfigureAwait(false);
                if (evalutionCachedResult is not null)
                {
                    return evalutionCachedResult;
                }
            }

            var evaluationResult = await this.GetSwitchValue(feature, evaluationContext, cancellationToken).ConfigureAwait(false);
            var switchValue = evaluationResult is null ?
                default! :
                JsonSerializer.SerializeToUtf8Bytes(evaluationResult.SwitchValue);

            FeatureCacheOptions options = new ();
            foreach (var cache in this.featureEvaluationCaches)
            {
                await cache.SetItem(feature, cacheContext, switchValue, options, cancellationToken).ConfigureAwait(false);
            }

            return switchValue;
        }

        public Task<string[]> GetFeatures(CancellationToken cancellationToken = default)
        {
            return this.featureDefinitionProvider.GetFeatures(cancellationToken);
        }

        private static Task<bool> EvaluateFilter<TEvaluationContext>(
            IFeatureFilterMetadata filter,
            FeatureFilterEvaluationContext context,
            TEvaluationContext evaluationContext,
            CancellationToken cancellationToken = default)
        {
            return filter switch
            {
                IFeatureFilter featureFilter => featureFilter.IsOn(context, cancellationToken),
                IContextualFeatureFilter contextualFeatureFilter => contextualFeatureFilter.IsOn(context, evaluationContext, cancellationToken),
                _ => Task.FromResult(false)
            };
        }

        private string GetCacheContext<TEvaluationContext>(TEvaluationContext evaluationContext)
        {
            var exec = this.featureContextProvider.GetContext();
            if (exec is null && evaluationContext is null)
            {
                return string.Empty;
            }
            else
            {
                var bytesToHash = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    Exec = exec,
                    Eval = evaluationContext
                });

                using var hasher = SHA256.Create();
                var result = hasher.ComputeHash(bytesToHash);
                return BitConverter.ToString(result).Trim(new char[] { '-' });
            }
        }

        private async Task<EvaluationResult?> GetSwitchValue<TEvaluationContext>(
            string feature,
            TEvaluationContext evaluationContext,
            CancellationToken cancellationToken)
        {
            var featureDefinition = await this.featureDefinitionProvider.GetFeatureDefinition(feature, cancellationToken).ConfigureAwait(false);
            if (featureDefinition is null)
            {
                return null;
            }

            Task<bool> EvaluateGroupFilters(string? group) => this.EvaluateFilters(
                feature,
                featureDefinition.Filters.Where(x => x.Group == group),
                evaluationContext,
                cancellationToken);

            EvaluationResult evalutionResult = new ()
            {
                SwitchValue = featureDefinition.OffValue
            };

            if (featureDefinition.IsOn && await EvaluateGroupFilters(null).ConfigureAwait(false))
            {
                if (featureDefinition.FilterGroups.Count == 0)
                {
                    evalutionResult.SwitchValue = featureDefinition.OnValue;
                }
                else
                {
                    foreach (var group in featureDefinition.FilterGroups.Where(g => g.IsOn))
                    {
                        if (await EvaluateGroupFilters(group.Name).ConfigureAwait(false))
                        {
                            evalutionResult.SwitchValue = group.OnValue;
                            break;
                        }
                    }
                }
            }

            return evalutionResult;
        }

        private async Task<bool> EvaluateFilters<TEvaluationContext>(string feature, IEnumerable<FeatureFilterDefinition> filters, TEvaluationContext evaluationContext, CancellationToken cancellationToken)
        {
            foreach (var featureFilterDefinition in filters)
            {
                var filter = this.GetFeatureFilter(featureFilterDefinition);

                FeatureFilterEvaluationContext context = new (feature, featureFilterDefinition.Settings);
                var isOn = await EvaluateFilter(filter, context, evaluationContext, cancellationToken).ConfigureAwait(false);
                if (!isOn)
                {
                    return false;
                }
            }

            return true;
        }

        private IFeatureFilterMetadata GetFeatureFilter(FeatureFilterDefinition featureFilterConfiguration)
        {
            return this.featureFilterMetadataCache.GetOrAdd(featureFilterConfiguration.Name, name =>
            {
                var ff = this.filters.FirstOrDefault(x => x.Name == name);
                return ff ?? throw new InvalidOperationException($"Unknown feature filter {name}");
            });
        }

        private class EvaluationResult
        {
            public object? SwitchValue { get; set; }
        }
    }
}