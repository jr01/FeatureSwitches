namespace FeatureSwitches.Filters
{
    public interface IContextualFeatureFilter : IFeatureFilterMetadata
    {
        bool IsEnabled(FeatureFilterEvaluationContext context, object? evaluationContext);
    }
}
