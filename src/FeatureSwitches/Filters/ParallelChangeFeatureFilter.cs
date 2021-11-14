namespace FeatureSwitches.Filters;

public class ParallelChangeFeatureFilter : ContextualFeatureFilter<ParallelChange>
{
    public override string Name => "ParallelChange";

    public override Task<bool> IsOn(FeatureFilterEvaluationContext context, ParallelChange evaluationContext, CancellationToken cancellationToken = default)
    {
        var settings = context.GetSettings<ParallelChange>();
        var isOn = settings switch
        {
            ParallelChange.Expanded =>
                evaluationContext == ParallelChange.Expanded,
            ParallelChange.Migrated =>
                evaluationContext is ParallelChange.Expanded or
                ParallelChange.Migrated,
            ParallelChange.Contracted =>
                evaluationContext is ParallelChange.Expanded or
                ParallelChange.Migrated or
                ParallelChange.Contracted,
            _ => throw new InvalidOperationException(),
        };

        return Task.FromResult(isOn);
    }
}
