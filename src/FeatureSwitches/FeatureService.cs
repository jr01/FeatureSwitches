using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

        public bool IsEnabled(string feature) =>
            this.GetValue<bool>(feature);

        public bool IsEnabled<TEvaluationContext>(string feature, TEvaluationContext evaluationContext) =>
            this.GetValue<bool, TEvaluationContext>(feature, evaluationContext);

        public TFeatureType GetValue<TFeatureType>(string feature) =>
            this.GetValue<TFeatureType, object>(feature, null!);

        public TFeatureType GetValue<TFeatureType, TEvaluationContext>(string feature, TEvaluationContext evaluationContext)
        {
            var sessionContextValue = JsonSerializer.Serialize(new
            {
                Exec = this.featureContextProvider.GetContext(),
                Eval = evaluationContext
            });

            if (this.featureEvaluationCache.TryGetValue(feature, sessionContextValue, out TFeatureType switchValue))
            {
                return switchValue;
            }

            switchValue = default!;
            if (this.GetSerializedSwitchValue(feature, evaluationContext, out var serializedSwitchValue))
            {
                try
                {
                    switchValue = JsonSerializer.Deserialize<TFeatureType>(serializedSwitchValue);
                }
                catch (JsonException)
                {
                }
            }

            this.featureEvaluationCache.AddOrUpdate(feature, sessionContextValue, switchValue);

            return switchValue;
        }

        public string[] GetFeatures()
        {
            return this.featureDefinitionProvider.GetFeatures();
        }

        private static bool EvaluateFilter<TEvaluationContext>(IFeatureFilterMetadata filter, FeatureFilterEvaluationContext context, TEvaluationContext evaluationContext)
        {
            if (filter is IFeatureFilter featureFilter)
            {
                return featureFilter.IsEnabled(context);
            }

            if (filter is IContextualFeatureFilter contextualFeatureFilter)
            {
                return contextualFeatureFilter.IsEnabled(context, evaluationContext);
            }

            return false;
        }

        private bool GetSerializedSwitchValue<TEvaluationContext>(
            string feature,
            TEvaluationContext evaluationContext,
            out byte[] serializedSwitchValue)
        {
            var featureDefinition = this.featureDefinitionProvider.GetFeatureDefinition(feature);
            if (featureDefinition == null)
            {
                serializedSwitchValue = default!;
                return false;
            }

            serializedSwitchValue = featureDefinition.OffValue;

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
                    var isEnabled = EvaluateFilter(filter, context, evaluationContext);
                    if (isEnabled)
                    {
                        serializedSwitchValue = featureFilterDefinition.Group.OnValue;
                    }
                    else
                    {
                        serializedSwitchValue = featureDefinition.OffValue;
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

            return true;
        }

        private IFeatureFilterMetadata GetFeatureFilter(FeatureFilterDefinition featureFilterConfiguration)
        {
            return this.featureFilterMetadataCache.GetOrAdd(featureFilterConfiguration.Type, name =>
            {
                var ff = this.filters.FirstOrDefault(x => x.Name == name);
                return ff ?? throw new InvalidOperationException($"Unknown feature filter {name}");
            });
        }
    }
}