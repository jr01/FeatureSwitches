using System;

namespace FeatureSwitches.Filters
{
    public class ParallelChangeFeatureFilter : ContextualFeatureFilter<ParallelChange>
    {
        public override string Name => "ParallelChange";

        public override bool IsEnabled(FeatureFilterEvaluationContext context, ParallelChange evaluationContext)
        {
            var scalar = context.GetSettings<ScalarValueSetting<ParallelChange>>();
            return scalar.Setting switch
            {
                ParallelChange.Expanded =>
                    evaluationContext == ParallelChange.Expanded,
                ParallelChange.Migrated =>
                    evaluationContext == ParallelChange.Expanded ||
                    evaluationContext == ParallelChange.Migrated,
                ParallelChange.Contracted =>
                    evaluationContext == ParallelChange.Expanded ||
                    evaluationContext == ParallelChange.Migrated ||
                    evaluationContext == ParallelChange.Contracted,
                _ => throw new InvalidOperationException(),
            };
        }
    }
}
