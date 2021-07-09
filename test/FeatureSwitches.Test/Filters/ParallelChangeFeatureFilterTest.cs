using System.Threading.Tasks;
using FeatureSwitches.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureSwitches.Test.Filters
{
    [TestClass]
    public class ParallelChangeFeatureFilterTest
    {
        [TestMethod]
        public async Task ParallelChangeFilter_state_matches_setting()
        {
            var filter = new ParallelChangeFeatureFilter();

            var context = GetContext(ParallelChange.Expanded);

            Assert.IsTrue(await filter.IsOn(context, ParallelChange.Expanded));
            Assert.IsFalse(await filter.IsOn(context, ParallelChange.Migrated));
            Assert.IsFalse(await filter.IsOn(context, ParallelChange.Contracted));

            context = GetContext(ParallelChange.Migrated);

            Assert.IsTrue(await filter.IsOn(context, ParallelChange.Expanded));
            Assert.IsTrue(await filter.IsOn(context, ParallelChange.Migrated));
            Assert.IsFalse(await filter.IsOn(context, ParallelChange.Contracted));

            context = GetContext(ParallelChange.Contracted);

            Assert.IsTrue(await filter.IsOn(context, ParallelChange.Expanded));
            Assert.IsTrue(await filter.IsOn(context, ParallelChange.Migrated));
            Assert.IsTrue(await filter.IsOn(context, ParallelChange.Contracted));
        }

        private static FeatureFilterEvaluationContext GetContext(ParallelChange parallelChange)
        {
            return new FeatureFilterEvaluationContext("A", parallelChange);
        }
    }
}
