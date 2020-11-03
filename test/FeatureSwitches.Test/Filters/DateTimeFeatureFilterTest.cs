using System;
using System.Globalization;
using System.Text.Json;
using FeatureSwitches.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureSwitches.Test.Filters
{
    [TestClass]
    public class DateTimeFeatureFilterTest
    {
        [TestMethod]
        public void DateTimeFilter_with_from()
        {
            var now = DateTimeOffset.Parse("2020-11-03", CultureInfo.InvariantCulture);
            var filter = new DateTimeFeatureFilter(() => { return now; });

            var context = GetContext(new DateTimeFeatureFilterSettings
            {
                From = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture)
            });

            Assert.IsFalse(filter.IsEnabled(context));
            now = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture);
            Assert.IsTrue(filter.IsEnabled(context));
            now = DateTimeOffset.Parse("2020-11-10", CultureInfo.InvariantCulture);
            Assert.IsTrue(filter.IsEnabled(context));
        }

        [TestMethod]
        public void DateTimeFilter_with_to()
        {
            var now = DateTimeOffset.Parse("2020-11-03", CultureInfo.InvariantCulture);
            var filter = new DateTimeFeatureFilter(() => { return now; });
            var context = GetContext(new DateTimeFeatureFilterSettings
            {
                To = DateTimeOffset.Parse("2020-11-10", CultureInfo.InvariantCulture)
            });

            Assert.IsTrue(filter.IsEnabled(context));
            now = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture);
            Assert.IsTrue(filter.IsEnabled(context));
            now = DateTimeOffset.Parse("2020-11-11", CultureInfo.InvariantCulture);
            Assert.IsFalse(filter.IsEnabled(context));
        }

        [TestMethod]
        public void DateTimeFilter_with_from_and_to()
        {
            var now = DateTimeOffset.Parse("2020-11-03", CultureInfo.InvariantCulture);
            var filter = new DateTimeFeatureFilter(() => { return now; });
            var context = GetContext(new DateTimeFeatureFilterSettings
            {
                From = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture),
                To = DateTimeOffset.Parse("2020-11-10", CultureInfo.InvariantCulture)
            });

            Assert.IsFalse(filter.IsEnabled(context));
            now = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture);
            Assert.IsTrue(filter.IsEnabled(context));
            now = DateTimeOffset.Parse("2020-11-10", CultureInfo.InvariantCulture);
            Assert.IsTrue(filter.IsEnabled(context));
            now = DateTimeOffset.Parse("2020-11-11", CultureInfo.InvariantCulture);
            Assert.IsFalse(filter.IsEnabled(context));
        }

        private static FeatureFilterEvaluationContext GetContext(DateTimeFeatureFilterSettings settings)
        {
            return new FeatureFilterEvaluationContext("A", JsonSerializer.SerializeToUtf8Bytes(settings));
        }
    }
}
