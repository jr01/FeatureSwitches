namespace FeatureSwitches.Filters;

public sealed class SessionFeatureFilter : IFeatureFilter
{
    private readonly SessionFeatureContext sessionContext;

    public SessionFeatureFilter(SessionFeatureContext sessionContext)
    {
        this.sessionContext = sessionContext;
    }

    public string Name => "Session";

    public Task<bool> IsOn(FeatureFilterEvaluationContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var settings = context.GetSettings<SessionFeatureFilterSettings>()
            ?? throw new InvalidOperationException("Invalid settings.");
        var isOn = DateTimeOffset.Compare(this.sessionContext.LoginTime, settings.From) >= 0;
        return Task.FromResult(isOn);
    }
}
