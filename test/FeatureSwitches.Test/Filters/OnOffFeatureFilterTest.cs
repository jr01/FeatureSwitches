using System.Text.Json;
using System.Threading.Tasks;
using FeatureSwitches.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureSwitches.Test.Filters
{
    [TestClass]
    public class OnOffFeatureFilterTest
    {
        [TestMethod]
        public async Task OnOffFilter_is_on_or_off()
        {
            var filter = new OnOffFeatureFilter();

            Assert.IsTrue(await filter.IsEnabled(GetContext(true)).ConfigureAwait(false));

            Assert.IsFalse(await filter.IsEnabled(GetContext(false)).ConfigureAwait(false));
        }

        private static FeatureFilterEvaluationContext GetContext(bool value)
        {
            var settings = new ScalarValueSetting<bool>(value);
            var evaluationContext = new FeatureFilterEvaluationContext("A", JsonSerializer.SerializeToUtf8Bytes(settings));
            return evaluationContext;
        }
    }
}
