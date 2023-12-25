using System.Security.Claims;
using FeatureSwitches.Filters;

namespace FeatureSwitches.Test.IntegrationTest;

public sealed class CustomerFeatureFilter : IFeatureFilter
{
    private readonly CurrentCustomer currentCustomer;

    public CustomerFeatureFilter(CurrentCustomer currentCustomer)
    {
        this.currentCustomer = currentCustomer;
    }

    public string Name => "Customer";

    public Task<bool> IsOn(FeatureFilterEvaluationContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var settings = context.GetSettings<CustomerFeatureFilterSettings>();

        var name = this.currentCustomer.Name ?? GetCurrentCustomer();
        if (name is null)
        {
            return Task.FromResult(false);
        }

        var isOn = settings?.Customers.Contains(name) ?? false;
        return Task.FromResult(isOn);
    }

    private static string? GetCurrentCustomer()
    {
        var identity = Thread.CurrentPrincipal?.Identity as ClaimsIdentity;
        return identity?.Name;
    }
}
