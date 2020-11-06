namespace FeatureSwitches.Caching
{
    public interface IFeatureCacheContextAccessor
    {
        object? GetContext();
    }
}
