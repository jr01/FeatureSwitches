using System.Collections.Generic;

namespace FeatureSwitches.Definitions
{
    public class FeatureDefinition
    {
#pragma warning disable CA2227 // Collection properties should be read only
        public IList<FeatureFilterDefinition> FeatureFilters { get; set; } = new List<FeatureFilterDefinition>();
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the off value as UTF-8 encoded JSON.
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public byte[] OffValue { get; set; } = null!;
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
