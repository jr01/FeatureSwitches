namespace FeatureSwitches.Caching;

public sealed class EvaluationCacheResult<TFeatureType>
{
    public TFeatureType Result { get; set; } = default!;
}
