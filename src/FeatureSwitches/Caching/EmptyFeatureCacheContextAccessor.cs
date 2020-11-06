namespace FeatureSwitches.Caching
{
    public class EmptyFeatureCacheContextAccessor : IFeatureCacheContextAccessor
    {
        public object? GetContext()
        {
            return null;
        }
    }
}
