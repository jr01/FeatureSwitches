using System.Text.Json;
using FeatureSwitches.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureSwitches.Test.Filters
{
    [TestClass]
    public class OnOffFeatureFilterTest
    {
        [TestMethod]
        public void OnOffFilter_is_on_or_off()
        {
            var filter = new OnOffFeatureFilter();

            Assert.IsTrue(filter.IsEnabled(GetContext(true)));

            Assert.IsFalse(filter.IsEnabled(GetContext(false)));
        }

        private static FeatureFilterEvaluationContext GetContext(bool value)
        {
            var settings = new ScalarValueSetting<bool>(value);
            var evaluationContext = new FeatureFilterEvaluationContext("A", JsonSerializer.SerializeToUtf8Bytes(settings));
            return evaluationContext;
        }
    }
}
