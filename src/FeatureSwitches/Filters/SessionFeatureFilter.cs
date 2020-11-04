using System;
using System.Threading.Tasks;

namespace FeatureSwitches.Filters
{
    public class SessionFeatureFilter : IFeatureFilter
    {
        private readonly SessionFeatureContext sessionContext;

        public SessionFeatureFilter(SessionFeatureContext sessionContext)
        {
            this.sessionContext = sessionContext;
        }

        public string Name => "Session";

        public Task<bool> IsEnabled(FeatureFilterEvaluationContext context)
        {
            var settings = context.GetSettings<SessionFeatureFilterSettings>();
            var isEnabled = DateTimeOffset.Compare(this.sessionContext.LoginTime, settings.From) >= 0;
            return Task.FromResult(isEnabled);
        }
    }
}
