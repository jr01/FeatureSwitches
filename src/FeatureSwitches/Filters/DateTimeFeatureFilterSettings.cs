using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureSwitches.Filters
{
    public class DateTimeFeatureFilterSettings
    {
        public DateTimeOffset? From { get; set; }

        public DateTimeOffset? To { get; set; }
    }
}
