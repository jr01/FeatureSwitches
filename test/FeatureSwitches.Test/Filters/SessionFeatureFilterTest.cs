using System.Globalization;
using FeatureSwitches.Filters;

namespace FeatureSwitches.Test.Filters;

[TestClass]
public class SessionFeatureFilterTest
{
    [TestMethod]
    public async Task SessionFilter_enables_at_or_after_login_time()
    {
        var context = new SessionFeatureContext();
        var filter = new SessionFeatureFilter(context);

        var settings = new SessionFeatureFilterSettings { From = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture) };
        var evaluationContext = new FeatureFilterEvaluationContext("A", settings);

        context.LoginTime = DateTimeOffset.Parse("2020-11-03", CultureInfo.InvariantCulture);
        Assert.IsFalse(await filter.IsOn(evaluationContext));
        context.LoginTime = DateTimeOffset.Parse("2020-11-04", CultureInfo.InvariantCulture);
        Assert.IsTrue(await filter.IsOn(evaluationContext));
        context.LoginTime = DateTimeOffset.Parse("2020-11-05", CultureInfo.InvariantCulture);
        Assert.IsTrue(await filter.IsOn(evaluationContext));
    }
}
