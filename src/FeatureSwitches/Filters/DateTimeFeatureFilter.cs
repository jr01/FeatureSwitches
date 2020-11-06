using System;
using System.Threading;
using System.Threading.Tasks;

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

        public Task<bool> IsEnabled(FeatureFilterEvaluationContext context, CancellationToken cancellationToken = default)
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

            return Task.FromResult(isEnabled);
        }
    }
}
