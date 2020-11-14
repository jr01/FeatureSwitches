using System;
using FeatureSwitches.Caching;
using FeatureSwitches.Definitions;
using FeatureSwitches.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FeatureSwitches
{
    public static class ServiceCollectionExtensions
    {
        public static void AddFeatureSwitches(this IServiceCollection serviceCollection, bool addScopedCache = false)
        {
            serviceCollection.AddSingleton<IFeatureFilterMetadata, ParallelChangeFeatureFilter>();
            serviceCollection.AddScoped<IFeatureFilterMetadata, SessionFeatureFilter>();
            serviceCollection.AddScoped<SessionFeatureContext>();

            serviceCollection.AddScoped<FeatureService>();

            if (addScopedCache)
            {
                serviceCollection.AddScoped<IFeatureCache, InMemoryFeatureCache>();
            }

            // Add required services, but only if not already registered.
            serviceCollection.TryAddSingleton<InMemoryFeatureDefinitionProvider>();
            serviceCollection.TryAddSingleton<IFeatureDefinitionProvider>(sp => sp.GetRequiredService<InMemoryFeatureDefinitionProvider>());
            serviceCollection.TryAddSingleton<IFeatureCacheContextAccessor, EmptyFeatureCacheContextAccessor>();
        }
    }
}