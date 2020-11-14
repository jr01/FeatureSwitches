﻿using System;
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
            var rawValue = await this.GetRawValue(feature, evaluationContext, cancellationToken).ConfigureAwait(false);
            if (rawValue is null)
            {
                return default!;
            }

            return JsonSerializer.Deserialize<TFeatureType>(rawValue);
        }

        public Task<byte[]?> GetRawValue(string feature, CancellationToken cancellationToken = default) =>
            this.GetRawValue<object>(feature, null!, cancellationToken);

        public async Task<byte[]?> GetRawValue<TEvaluationContext>(
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

            byte[]? switchValue = default!;
            var evaluationResult = await this.GetSerializedSwitchValue(feature, evaluationContext, cancellationToken).ConfigureAwait(false);
            if (evaluationResult.IsOn)
            {
                switchValue = evaluationResult.SerializedSwitchValue;
            }

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

        private async Task<EvaluationResult> GetSerializedSwitchValue<TEvaluationContext>(
            string feature,
            TEvaluationContext evaluationContext,
            CancellationToken cancellationToken)
        {
            EvaluationResult evalutionResult = new ()
            {
                IsOn = false
            };

            var featureDefinition = await this.featureDefinitionProvider.GetFeatureDefinition(feature, cancellationToken).ConfigureAwait(false);
            if (featureDefinition is null)
            {
                return evalutionResult;
            }

            evalutionResult.SerializedSwitchValue = featureDefinition.OffValue;
            if (!featureDefinition.IsOn)
            {
                return evalutionResult;
            }

            var filterGroups = featureDefinition.FeatureFilters.GroupBy(x => x.Group?.Name);
            foreach (var filterGrouping in filterGroups)
            {
                // Filtergroups are OR'ed, except for the null group.
                // All filters within 1 group are AND'ed.
                var groupIsOn = true;
                foreach (var featureFilterDefinition in filterGrouping)
                {
                    if (featureFilterDefinition.Group is not null && !featureFilterDefinition.Group.IsOn)
                    {
                        groupIsOn = false;
                        break;
                    }

                    var filter = this.GetFeatureFilter(featureFilterDefinition);

                    FeatureFilterEvaluationContext context = new (feature, featureFilterDefinition.Config);
                    var isOn = await EvaluateFilter(filter, context, evaluationContext, cancellationToken).ConfigureAwait(false);
                    if (isOn)
                    {
                        evalutionResult.SerializedSwitchValue = featureFilterDefinition.Group is not null ?
                            featureFilterDefinition.Group.OnValue :
                            featureDefinition.OnValue;
                    }
                    else
                    {
                        evalutionResult.SerializedSwitchValue = featureDefinition.OffValue;
                        groupIsOn = false;
                        break;
                    }
                }

                if ((groupIsOn && filterGrouping.Key is not null) ||
                    (!groupIsOn && filterGrouping.Key is null))
                {
                    // null group is always AND'ed
                    break;
                }
            }

            evalutionResult.IsOn = true;
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
            public bool IsOn { get; set; }

            public byte[] SerializedSwitchValue { get; set; } = null!;
        }
    }
}