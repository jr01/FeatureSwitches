namespace FeatureSwitches.Filters;

public class SessionFeatureFilter : IFeatureFilter
{
    private readonly SessionFeatureContext sessionContext;

    public SessionFeatureFilter(SessionFeatureContext sessionContext)
    {
        this.sessionContext = sessionContext;
    }

    public string Name => "Session";

    public Task<bool> IsOn(FeatureFilterEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var settings = context.GetSettings<SessionFeatureFilterSettings>();
        if (settings is null)
        {
            throw new InvalidOperationException("Invalid settings.");
        }

        var isOn = DateTimeOffset.Compare(this.sessionContext.LoginTime, settings.From) >= 0;
        return Task.FromResult(isOn);
    }
}
