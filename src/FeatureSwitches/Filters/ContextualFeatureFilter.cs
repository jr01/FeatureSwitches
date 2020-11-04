using System.Threading.Tasks;

namespace FeatureSwitches.Filters
{
    public abstract class ContextualFeatureFilter<TEvaluationContext> : IContextualFeatureFilter
    {
        public abstract string Name { get; }

        public Task<bool> IsEnabled(FeatureFilterEvaluationContext context, object? evaluationContext)
        {
            evaluationContext ??= default(TEvaluationContext);

            return this.IsEnabled(context, (TEvaluationContext)evaluationContext!);
        }

        public abstract Task<bool> IsEnabled(FeatureFilterEvaluationContext context, TEvaluationContext evaluationContext);
    }
}
