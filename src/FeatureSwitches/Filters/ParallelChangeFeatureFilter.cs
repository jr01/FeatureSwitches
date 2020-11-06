﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace FeatureSwitches.Filters
{
    public class ParallelChangeFeatureFilter : ContextualFeatureFilter<ParallelChange>
    {
        public override string Name => "ParallelChange";

        public override Task<bool> IsEnabled(FeatureFilterEvaluationContext context, ParallelChange evaluationContext, CancellationToken cancellationToken = default)
        {
            var scalar = context.GetSettings<ScalarValueSetting<ParallelChange>>();
            var isEnabled = scalar.Setting switch
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

            return Task.FromResult(isEnabled);
        }
    }
}
