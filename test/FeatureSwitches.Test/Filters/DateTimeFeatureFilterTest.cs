using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using FeatureSwitches.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureSwitches.Test.Filters
{
    [TestClass]
    public class DateTimeFeatureFilterTest
    {
        [TestMethod]
        public async Task DateTimeFilter_with_from()
        {
            var now = DateTimeOffset.Parse("2020-11-03", CultureInfo.InvariantCulture);
            var filter = new DateTimeFeatureFilter(() => { return now; });

            var context = GetContext(new DateTimeFeatureFilterSettings
            {
                From = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture)
            });

            Assert.IsFalse(await filter.IsOn(context).ConfigureAwait(false));
            now = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture);
            Assert.IsTrue(await filter.IsOn(context).ConfigureAwait(false));
            now = DateTimeOffset.Parse("2020-11-10", CultureInfo.InvariantCulture);
            Assert.IsTrue(await filter.IsOn(context).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task DateTimeFilter_with_to()
        {
            var now = DateTimeOffset.Parse("2020-11-03", CultureInfo.InvariantCulture);
            var filter = new DateTimeFeatureFilter(() => { return now; });
            var context = GetContext(new DateTimeFeatureFilterSettings
            {
                To = DateTimeOffset.Parse("2020-11-10", CultureInfo.InvariantCulture)
            });

            Assert.IsTrue(await filter.IsOn(context).ConfigureAwait(false));
            now = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture);
            Assert.IsTrue(await filter.IsOn(context).ConfigureAwait(false));
            now = DateTimeOffset.Parse("2020-11-11", CultureInfo.InvariantCulture);
            Assert.IsFalse(await filter.IsOn(context).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task DateTimeFilter_with_from_and_to()
        {
            var now = DateTimeOffset.Parse("2020-11-03", CultureInfo.InvariantCulture);
            var filter = new DateTimeFeatureFilter(() => { return now; });
            var context = GetContext(new DateTimeFeatureFilterSettings
            {
                From = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture),
                To = DateTimeOffset.Parse("2020-11-10", CultureInfo.InvariantCulture)
            });

            Assert.IsFalse(await filter.IsOn(context).ConfigureAwait(false));
            now = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture);
            Assert.IsTrue(await filter.IsOn(context).ConfigureAwait(false));
            now = DateTimeOffset.Parse("2020-11-10", CultureInfo.InvariantCulture);
            Assert.IsTrue(await filter.IsOn(context).ConfigureAwait(false));
            now = DateTimeOffset.Parse("2020-11-11", CultureInfo.InvariantCulture);
            Assert.IsFalse(await filter.IsOn(context).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task DateTimeFilter_without_from_and_to()
        {
            var now = DateTimeOffset.Parse("2020-11-03", CultureInfo.InvariantCulture);
            var filter = new DateTimeFeatureFilter(() => { return now; });
            var context = GetContext(new DateTimeFeatureFilterSettings
            {
                From = null,
                To = null
            });

            Assert.IsTrue(await filter.IsOn(context).ConfigureAwait(false));
        }

        private static FeatureFilterEvaluationContext GetContext(DateTimeFeatureFilterSettings settings)
        {
            return new FeatureFilterEvaluationContext("A", JsonSerializer.SerializeToUtf8Bytes(settings));
        }
    }
}
