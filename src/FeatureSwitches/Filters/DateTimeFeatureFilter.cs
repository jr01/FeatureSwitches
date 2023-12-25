namespace FeatureSwitches.Filters;

public sealed class DateTimeFeatureFilter : IFeatureFilter
{
    private readonly Func<DateTimeOffset> dateTimeResolver;

    public DateTimeFeatureFilter()
        : this(() => DateTimeOffset.UtcNow)
    {
    }

    public DateTimeFeatureFilter(Func<DateTimeOffset> dateTimeResolver)
    {
        this.dateTimeResolver = dateTimeResolver;
    }

    public string Name => "DateTime";

    public Task<bool> IsOn(FeatureFilterEvaluationContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var now = this.dateTimeResolver();

        var settings = context.GetSettings<DateTimeFeatureFilterSettings>()
            ?? throw new InvalidOperationException("Invalid settings.");
        var isOn = true;
        if (settings.From.HasValue && now < settings.From)
        {
            isOn = false;
        }

        if (settings.To.HasValue && now > settings.To)
        {
            isOn = false;
        }

        return Task.FromResult(isOn);
    }
}
