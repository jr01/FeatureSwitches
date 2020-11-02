using System.Security.Claims;
using System.Threading;
using FeatureSwitches.EvaluationCaching;

namespace FeatureSwitches.Test.IntegrationTest
{
    public class FeatureContextAccessor : IEvaluationContextAccessor
    {
        private readonly CurrentCustomer currentCustomer;

        public FeatureContextAccessor(CurrentCustomer currentCustomer)
        {
            this.currentCustomer = currentCustomer;
        }

        public object? GetContext()
        {
            var name = this.currentCustomer.Name;
            if (name == null)
            {
                var identity = Thread.CurrentPrincipal?.Identity as ClaimsIdentity;
                name = identity?.Name;
            }

            return name;
        }
    }
}
