using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FeatureSwitches.Definitions;
using FeatureSwitches.EvaluationCaching;
using FeatureSwitches.Filters;

namespace FeatureSwitches
{
    /// <summary>
    /// A featureservice implementation. Typically has as scoped lifetime.
    /// </summary>
    public class FeatureService : IFeatureService
    {
        private readonly IFeatureDefinitionProvider featureDefinitionProvider;
        private readonly IEnumerable<IFeatureFilterMetadata> filters;
        private readonly IFeatureEvaluationCache featureEvaluationCache;
        private readonly IEvaluationContextAccessor featureContextProvider;
        private readonly ConcurrentDictionary<string, IFeatureFilterMetadata> featureFilterMetadataCache =
            new ConcurrentDictionary<string, IFeatureFilterMetadata>();

        public FeatureService(
            IFeatureDefinitionProvider featureDefinitionProvider,
            IFeatureEvaluationCache featureEvaluationCache,
            IEvaluationContextAccessor featureContextProvider,
            IEnumerable<IFeatureFilterMetadata> filters)
        {
            this.featureDefinitionProvider = featureDefinitionProvider;
            this.filters = filters;
            this.featureEvaluationCache = featureEvaluationCache;
            this.featureContextProvider = featureContextProvider;
        }

        public Task<bool> IsEnabled(string feature) =>
            this.GetValue<bool>(feature);

        public Task<bool> IsEnabled<TEvaluationContext>(string feature, TEvaluationContext evaluationContext) =>
            this.GetValue<bool, TEvaluationContext>(feature, evaluationContext);

        public Task<TFeatureType> GetValue<TFeatureType>(string feature) =>
            this.GetValue<TFeatureType, object>(feature, null!);

        public async Task<TFeatureType> GetValue<TFeatureType, TEvaluationContext>(string feature, TEvaluationContext evaluationContext)
        {
            var sessionContextValue = JsonSerializer.Serialize(new
            {
                Exec = this.featureContextProvider.GetContext(),
                Eval = evaluationContext
            });

            var evalutionCachedResult = await this.featureEvaluationCache.GetItem<TFeatureType>(feature, sessionContextValue).ConfigureAwait(false);
            if (evalutionCachedResult != null)
            {
                return evalutionCachedResult.Result;
            }

            TFeatureType switchValue = default!;
            var evaluationResult = await this.GetSerializedSwitchValue(feature, evaluationContext).ConfigureAwait(false);
            if (evaluationResult.IsEnabled)
            {
                try
                {
                    switchValue = JsonSerializer.Deserialize<TFeatureType>(evaluationResult.SerializedSwitchValue);
                }
                catch (JsonException)
                {
                }
            }

            await this.featureEvaluationCache.SetItem(feature, sessionContextValue, switchValue).ConfigureAwait(false);

            return switchValue;
        }

        public Task<string[]> GetFeatures()
        {
            return this.featureDefinitionProvider.GetFeatures();
        }

        private static Task<bool> EvaluateFilter<TEvaluationContext>(IFeatureFilterMetadata filter, FeatureFilterEvaluationContext context, TEvaluationContext evaluationContext)
        {
            if (filter is IFeatureFilter featureFilter)
            {
                return featureFilter.IsEnabled(context);
            }

            if (filter is IContextualFeatureFilter contextualFeatureFilter)
            {
                return contextualFeatureFilter.IsEnabled(context, evaluationContext);
            }

            return Task.FromResult(false);
        }

        private async Task<EvaluationResult> GetSerializedSwitchValue<TEvaluationContext>(
            string feature,
            TEvaluationContext evaluationContext)
        {
            var evalutionResult = new EvaluationResult
            {
                IsEnabled = false
            };

            var featureDefinition = await this.featureDefinitionProvider.GetFeatureDefinition(feature).ConfigureAwait(false);
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
                    var isEnabled = await EvaluateFilter(filter, context, evaluationContext).ConfigureAwait(false);
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