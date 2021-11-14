namespace FeatureSwitches.Filters;

public interface IFeatureFilter : IFeatureFilterMetadata
{
    Task<bool> IsOn(FeatureFilterEvaluationContext context, CancellationToken cancellationToken = default);
}
