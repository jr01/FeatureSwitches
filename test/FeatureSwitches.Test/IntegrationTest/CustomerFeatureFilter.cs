using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FeatureSwitches.Filters;

namespace FeatureSwitches.Test.IntegrationTest
{
    public class CustomerFeatureFilter : IFeatureFilter
    {
        private readonly CurrentCustomer currentCustomer;

        public CustomerFeatureFilter(CurrentCustomer currentCustomer)
        {
            this.currentCustomer = currentCustomer;
        }

        public string Name => "Customer";

        public Task<bool> IsEnabled(FeatureFilterEvaluationContext context, CancellationToken cancellationToken = default)
        {
            var settings = context.GetSettings<CustomerFeatureFilterSettings>();

            var name = this.currentCustomer.Name ?? GetCurrentCustomer();
            if (name == null)
            {
                return Task.FromResult(false);
            }

            var isEnabled = settings?.Customers.Contains(name) ?? false;
            return Task.FromResult(isEnabled);
        }

        private static string? GetCurrentCustomer()
        {
            var identity = Thread.CurrentPrincipal?.Identity as ClaimsIdentity;
            return identity?.Name;
        }
    }
}
