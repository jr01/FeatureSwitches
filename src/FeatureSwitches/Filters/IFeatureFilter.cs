namespace FeatureSwitches.Filters
{
    public interface IFeatureFilter : IFeatureFilterMetadata
    {
        bool IsEnabled(FeatureFilterEvaluationContext context);
    }
}