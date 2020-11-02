using System;
using System.Globalization;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FeatureSwitches.Filters;

namespace FeatureSwitches.Test.Filters
{
    [TestClass]
    public class SessionFeatureFilterTest
    {
        [TestMethod]
        public void SessionFilter_enables_at_or_after_login_time()
        {
            var context = new SessionFeatureContext();
            var filter = new SessionFeatureFilter(context);

            var settings = new SessionFeatureFilterSettings { From = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture) };
            var evaluationContext = new FeatureFilterEvaluationContext("A", JsonSerializer.SerializeToUtf8Bytes(settings));

            context.LoginTime = DateTimeOffset.Parse("2020-11-03", CultureInfo.InvariantCulture);
            Assert.IsFalse(filter.IsEnabled(evaluationContext));
            context.LoginTime = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture);
            Assert.IsTrue(filter.IsEnabled(evaluationContext));
            context.LoginTime = DateTimeOffset.Parse("2020-11-05", CultureInfo.InvariantCulture);
            Assert.IsTrue(filter.IsEnabled(evaluationContext));
        }
    }
}
