namespace FeatureSwitches.Filters
{
    public abstract class ContextualFeatureFilter<TEvaluationContext> : IContextualFeatureFilter
    {
        public abstract string Name { get; }

        public bool IsEnabled(FeatureFilterEvaluationContext context, object? evaluationContext)
        {
            evaluationContext ??= default(TEvaluationContext);

            return this.IsEnabled(context, (TEvaluationContext)evaluationContext!);
        }

        public abstract bool IsEnabled(FeatureFilterEvaluationContext context, TEvaluationContext evaluationContext);
    }
}
