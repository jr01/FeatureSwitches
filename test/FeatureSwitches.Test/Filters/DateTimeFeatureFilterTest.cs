using System.Globalization;
using FeatureSwitches.Filters;

namespace FeatureSwitches.Test.Filters;

[TestClass]
public sealed class DateTimeFeatureFilterTest
{
    [TestMethod]
    public async Task DateTimeFilter_with_from()
    {
        var now = DateTimeOffset.Parse("2020-11-03", CultureInfo.InvariantCulture);
        var filter = new DateTimeFeatureFilter(() => { return now; });

        var context = GetContext(new DateTimeFeatureFilterSettings
        {
            From = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture),
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
            To = DateTimeOffset.Parse("2020-11-10", CultureInfo.InvariantCulture),
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
            To = DateTimeOffset.Parse("2020-11-10", CultureInfo.InvariantCulture),
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
            To = null,
        });

        Assert.IsTrue(await filter.IsOn(context).ConfigureAwait(false));
    }

    [TestMethod]
    public void Deserialize_with_Uppercased_property_names()
    {
        var settings = new
        {
            From = DateTimeOffset.Parse("2020-11-21T00:00:00.000Z", CultureInfo.InvariantCulture),
            To = (DateTimeOffset?)null,
        };

        var context = new FeatureFilterEvaluationContext("A", settings);

        var dateTimeFilterSettings = context.GetSettings<DateTimeFeatureFilterSettings>();
        Assert.IsNotNull(dateTimeFilterSettings);

        Assert.AreEqual(settings.From, dateTimeFilterSettings!.From);
        Assert.IsNull(dateTimeFilterSettings.To);
    }

    [TestMethod]
    public void Deserialize_with_lowercased_property_names()
    {
        var settings = new
        {
            from = DateTimeOffset.Parse("2020-11-21T00:00:00.000Z", CultureInfo.InvariantCulture),
            to = (DateTimeOffset?)null,
        };

        var context = new FeatureFilterEvaluationContext("A", settings);

        var dateTimeFilterSettings = context.GetSettings<DateTimeFeatureFilterSettings>();
        Assert.IsNotNull(dateTimeFilterSettings);

        Assert.AreEqual(settings.from, dateTimeFilterSettings!.From);
        Assert.IsNull(dateTimeFilterSettings.To);
    }

    private static FeatureFilterEvaluationContext GetContext(DateTimeFeatureFilterSettings settings)
    {
        return new FeatureFilterEvaluationContext("A", settings);
    }
}
