using System;

namespace FeatureSwitches.Filters
{
    public class DateTimeFeatureFilter : IFeatureFilter
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

        public bool IsEnabled(FeatureFilterEvaluationContext context)
        {
            var now = this.dateTimeResolver();

            var settings = context.GetSettings<DateTimeFeatureFilterSettings>();

            var isEnabled = true;
            if (settings.From.HasValue && now < settings.From)
            {
                isEnabled = false;
            }

            if (settings.To.HasValue && now > settings.To)
            {
                isEnabled = false;
            }

            return isEnabled;
        }
    }
}
