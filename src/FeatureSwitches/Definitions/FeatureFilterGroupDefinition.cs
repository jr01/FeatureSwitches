namespace FeatureSwitches.Definitions
{
    public class FeatureFilterGroupDefinition
    {
        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets whether the group is on.
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