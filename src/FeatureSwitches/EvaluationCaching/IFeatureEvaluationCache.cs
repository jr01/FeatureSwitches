namespace FeatureSwitches.EvaluationCaching
{
    public interface IFeatureEvaluationCache
    {
#pragma warning disable CA1021 // Avoid out parameters
        bool TryGetValue<T>(string feature, string sessionContextValue, out T value);
#pragma warning restore CA1021 // Avoid out parameters

        void AddOrUpdate<T>(string feature, string sessionContextValue, T value);
    }
}
