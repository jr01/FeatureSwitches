using System;

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

        public bool IsEnabled(FeatureFilterEvaluationContext context)
        {
            var settings = context.GetSettings<SessionFeatureFilterSettings>();
            return DateTimeOffset.Compare(this.sessionContext.LoginTime, settings.From) >= 0;
        }
    }
}
