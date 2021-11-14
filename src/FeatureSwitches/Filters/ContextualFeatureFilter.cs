namespace FeatureSwitches.Filters;

public abstract class ContextualFeatureFilter<TEvaluationContext> : IContextualFeatureFilter
{
    public abstract string Name { get; }

    public Task<bool> IsOn(FeatureFilterEvaluationContext context, object? evaluationContext, CancellationToken cancellationToken = default)
    {
        evaluationContext ??= default(TEvaluationContext);

        return this.IsOn(context, (TEvaluationContext)evaluationContext!, cancellationToken);
    }

    public abstract Task<bool> IsOn(FeatureFilterEvaluationContext context, TEvaluationContext evaluationContext, CancellationToken cancellationToken = default);
}
