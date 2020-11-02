using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FeatureSwitches.Definitions;
using FeatureSwitches.EvaluationCaching;
using FeatureSwitches.Filters;

namespace FeatureSwitches
{
    public static class ServiceCollectionExtensions
    {
        public static void AddFeatureSwitches(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IFeatureFilterMetadata, OnOffFeatureFilter>();
            serviceCollection.AddSingleton<IFeatureFilterMetadata, ParallelChangeFeatureFilter>();
            serviceCollection.AddScoped<IFeatureFilterMetadata, SessionFeatureFilter>();
            serviceCollection.AddScoped<SessionFeatureContext>();

            serviceCollection.AddScoped<FeatureService>();

            // Add required services, but only if not already registered.
            serviceCollection.TryAddSingleton<InMemoryFeatureDefinitionProvider>();
            serviceCollection.TryAddSingleton<IFeatureDefinitionProvider>(sp => sp.GetRequiredService<InMemoryFeatureDefinitionProvider>());
            serviceCollection.TryAddSingleton<IFeatureEvaluationCache, FeatureEvaluationCache>();
            serviceCollection.TryAddSingleton<IEvaluationContextAccessor, EmptyEvaluationContextAccessor>();
        }
    }
}