using System;

namespace FeatureSwitches.Definitions
{
    public class FeatureDefinitionChangeEventArgs : EventArgs
    {
        public FeatureDefinitionChangeEventArgs(string feature)
        {
            this.Feature = feature;
        }

        public string Feature { get; }
    }
}
