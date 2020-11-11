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
        private readonly ConcurrentDictionary<string, IFeatureFilterMetadata> featureFilterMetadataCache =
            new ConcurrentDictionary<string, IFeatureFilterMetadata>();

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

        public Task<bool> IsEnabled(string feature, CancellationToken cancellationToken = default) =>
            this.GetValue<bool>(feature, cancellationToken);

        public Task<bool> IsEnabled<TEvaluationContext>(string feature, TEvaluationContext evaluationContext, CancellationToken cancellationToken = default) =>
            this.GetValue<bool, TEvaluationContext>(feature, evaluationContext, cancellationToken);

        public Task<TFeatureType> GetValue<TFeatureType>(string feature, CancellationToken cancellationToken = default) =>
            this.GetValue<TFeatureType, object>(feature, null!, cancellationToken);

        public async Task<TFeatureType> GetValue<TFeatureType, TEvaluationContext>(
            string feature,
            TEvaluationContext evaluationContext,
            CancellationToken cancellationToken = default)
        {
            var rawValue = await this.GetRawValue<TEvaluationContext>(feature, evaluationContext, cancellationToken).ConfigureAwait(false);
            try
            {
                return JsonSerializer.Deserialize<TFeatureType>(rawValue);
            }
            catch (JsonException)
            {
            }

            return default!;
        }

        public Task<byte[]?> GetRawValue(string feature, CancellationToken cancellationToken = default) =>
            this.GetRawValue(feature, cancellationToken);

        public async Task<byte[]?> GetRawValue<TEvaluationContext>(
            string feature,
            TEvaluationContext evaluationContext,
            CancellationToken cancellationToken = default)
        {
            var cacheContext = this.GetCacheContext(evaluationContext);
            foreach (var cache in this.featureEvaluationCaches)
            {
                var evalutionCachedResult = await cache.GetItem(feature, cacheContext, cancellationToken).ConfigureAwait(false);
                if (evalutionCachedResult != null)
                {
                    return evalutionCachedResult;
                }
            }

            byte[]? switchValue = default!;
            var evaluationResult = await this.GetSerializedSwitchValue(feature, evaluationContext, cancellationToken).ConfigureAwait(false);
            if (evaluationResult.IsEnabled)
            {
                switchValue = evaluationResult.SerializedSwitchValue;
            }

            var options = new FeatureCacheOptions();
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

        private static Task<bool> EvaluateFilter<TEvaluationContext>(IFeatureFilterMetadata filter, FeatureFilterEvaluationContext context, TEvaluationContext evaluationContext, CancellationToken cancellationToken = default)
        {
            if (filter is IFeatureFilter featureFilter)
            {
                return featureFilter.IsEnabled(context, cancellationToken);
            }

            if (filter is IContextualFeatureFilter contextualFeatureFilter)
            {
                return contextualFeatureFilter.IsEnabled(context, evaluationContext, cancellationToken);
            }

            return Task.FromResult(false);
        }

        private string GetCacheContext<TEvaluationContext>(TEvaluationContext evaluationContext)
        {
            // ToDo: should this featureContextProvider really be here? Or should this
            //       be left upto a specific Cache implementation?
            //       It could even depend on a filter? If there is no 'customerfilter'
            //       attached to a feature then it can be cached regardless of customer.
            var exec = this.featureContextProvider.GetContext();
            if (exec == null && evaluationContext == null)
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

        private async Task<EvaluationResult> GetSerializedSwitchValue<TEvaluationContext>(
            string feature,
            TEvaluationContext evaluationContext,
            CancellationToken cancellationToken)
        {
            var evalutionResult = new EvaluationResult
            {
                IsEnabled = false
            };

            var featureDefinition = await this.featureDefinitionProvider.GetFeatureDefinition(feature, cancellationToken).ConfigureAwait(false);
            if (featureDefinition == null)
            {
                return evalutionResult;
            }

            evalutionResult.SerializedSwitchValue = featureDefinition.OffValue;

            var filterGroups = featureDefinition.FeatureFilters.GroupBy(x => x.Group.Name);
            foreach (var filterGrouping in filterGroups)
            {
                // Filtergroups are OR'ed, except for the null group.
                // All filters within 1 group are AND'ed.
                var groupEnabled = true;
                foreach (var featureFilterDefinition in filterGrouping)
                {
                    var filter = this.GetFeatureFilter(featureFilterDefinition);

                    var context = new FeatureFilterEvaluationContext(feature, featureFilterDefinition.Config);
                    var isEnabled = await EvaluateFilter(filter, context, evaluationContext, cancellationToken).ConfigureAwait(false);
                    if (isEnabled)
                    {
                        evalutionResult.SerializedSwitchValue = featureFilterDefinition.Group.OnValue;
                    }
                    else
                    {
                        evalutionResult.SerializedSwitchValue = featureDefinition.OffValue;
                        groupEnabled = false;
                        break;
                    }
                }

                if ((groupEnabled && filterGrouping.Key != null) ||
                    (!groupEnabled && filterGrouping.Key == null))
                {
                    // null group is always AND'ed
                    break;
                }
            }

            evalutionResult.IsEnabled = true;
            return evalutionResult;
        }

        private IFeatureFilterMetadata GetFeatureFilter(FeatureFilterDefinition featureFilterConfiguration)
        {
            return this.featureFilterMetadataCache.GetOrAdd(featureFilterConfiguration.Type, name =>
            {
                var ff = this.filters.FirstOrDefault(x => x.Name == name);
                return ff ?? throw new InvalidOperationException($"Unknown feature filter {name}");
            });
        }

        private class EvaluationResult
        {
            public bool IsEnabled { get; set; }

            public byte[] SerializedSwitchValue { get; set; } = null!;
        }
    }
}