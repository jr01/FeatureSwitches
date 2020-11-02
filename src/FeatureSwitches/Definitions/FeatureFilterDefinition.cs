namespace FeatureSwitches.Definitions
{
    public class FeatureFilterDefinition
    {
        public string Type { get; set; } = null!;

#pragma warning disable CA1819 // Properties should not return arrays
        public byte[] Config { get; set; } = null!;
#pragma warning restore CA1819 // Properties should not return arrays

        public FeatureFilterGroupDefinition Group { get; set; } = null!;
    }
}
