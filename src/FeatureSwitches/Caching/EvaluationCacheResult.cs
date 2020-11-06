namespace FeatureSwitches.Caching
{
    public class EvaluationCacheResult<TFeatureType>
    {
        public TFeatureType Result { get; set; } = default!;
    }
}
