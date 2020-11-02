using System.Security.Claims;
using System.Threading;
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

        public bool IsEnabled(FeatureFilterEvaluationContext context)
        {
            var settings = context.GetSettings<CustomerFeatureFilterSettings>();

            var name = this.currentCustomer.Name ?? GetCurrentCustomer();
            if (name == null)
            {
                return false;
            }

            return settings?.Customers.Contains(name) ?? false;
        }

        private static string? GetCurrentCustomer()
        {
            var identity = Thread.CurrentPrincipal?.Identity as ClaimsIdentity;
            return identity?.Name;
        }
    }
}
