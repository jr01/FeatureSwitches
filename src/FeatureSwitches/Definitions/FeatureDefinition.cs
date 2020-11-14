using System.Collections.Generic;

namespace FeatureSwitches.Definitions
{
    public class FeatureDefinition
    {
        /// <summary>
        /// Gets or sets a list of filters.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public IList<FeatureFilterDefinition> Filters { get; set; } = new List<FeatureFilterDefinition>();
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets a list of filter groups.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public IList<FeatureFilterGroupDefinition> FilterGroups { get; set; } = new List<FeatureFilterGroupDefinition>();
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the off value as UTF-8 encoded JSON.
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public byte[] OffValue { get; set; } = null!;
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets whether the feature is on.
        /// </summary>
        public bool IsOn { get; set; }

        /// <summary>
        /// Gets or sets the on value as UTF-8 encoded JSON.
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public byte[] OnValue { get; set; } = null!;
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
