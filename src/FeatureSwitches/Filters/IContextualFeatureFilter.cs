namespace FeatureSwitches.Filters;

public interface IContextualFeatureFilter : IFeatureFilterMetadata
{
    Task<bool> IsOn(FeatureFilterEvaluationContext context, object? evaluationContext, CancellationToken cancellationToken = default);
}
