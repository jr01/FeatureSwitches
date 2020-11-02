using System.Collections.Generic;

namespace FeatureSwitches.Test.IntegrationTest
{
    public class CustomerFeatureFilterSettings
    {
#pragma warning disable CA2227 // Collection properties should be read only
        public HashSet<string> Customers { get; set; } = null!;
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
