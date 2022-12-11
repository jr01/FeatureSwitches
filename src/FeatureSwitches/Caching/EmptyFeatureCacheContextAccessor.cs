namespace FeatureSwitches.Caching;

public sealed class EmptyFeatureCacheContextAccessor : IFeatureCacheContextAccessor
{
    public object? GetContext()
    {
        return null;
    }
}
