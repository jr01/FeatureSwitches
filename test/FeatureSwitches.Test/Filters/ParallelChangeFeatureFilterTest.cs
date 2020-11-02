using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FeatureSwitches.Filters;

namespace FeatureSwitches.Test.Filters
{
    [TestClass]
    public class ParallelChangeFeatureFilterTest
    {
        [TestMethod]
        public void ParallelChangeFilter_state_matches_setting()
        {
            var filter = new ParallelChangeFeatureFilter();

            var context = GetContext(ParallelChange.Expanded);

            Assert.IsTrue(filter.IsEnabled(context, ParallelChange.Expanded));
            Assert.IsFalse(filter.IsEnabled(context, ParallelChange.Migrated));
            Assert.IsFalse(filter.IsEnabled(context, ParallelChange.Contracted));

            context = GetContext(ParallelChange.Migrated);

            Assert.IsTrue(filter.IsEnabled(context, ParallelChange.Expanded));
            Assert.IsTrue(filter.IsEnabled(context, ParallelChange.Migrated));
            Assert.IsFalse(filter.IsEnabled(context, ParallelChange.Contracted));

            context = GetContext(ParallelChange.Contracted);

            Assert.IsTrue(filter.IsEnabled(context, ParallelChange.Expanded));
            Assert.IsTrue(filter.IsEnabled(context, ParallelChange.Migrated));
            Assert.IsTrue(filter.IsEnabled(context, ParallelChange.Contracted));
        }

        private static FeatureFilterEvaluationContext GetContext(ParallelChange parallelChange)
        {
            var settings = new ScalarValueSetting<ParallelChange>(parallelChange);
            return new FeatureFilterEvaluationContext("A", JsonSerializer.SerializeToUtf8Bytes(settings));
        }
    }
}
