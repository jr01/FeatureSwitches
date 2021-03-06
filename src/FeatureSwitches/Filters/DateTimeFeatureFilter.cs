﻿using System;
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

        public Task<bool> IsOn(FeatureFilterEvaluationContext context, CancellationToken cancellationToken = default)
        {
            var now = this.dateTimeResolver();

            var settings = context.GetSettings<DateTimeFeatureFilterSettings>();
            if (settings is null)
            {
                throw new InvalidOperationException("Invalid settings.");
            }

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
}
