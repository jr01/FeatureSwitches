using System;

namespace FeatureSwitches.MSTest
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class FeatureTestFilterAttribute : Attribute
    {
        public FeatureTestFilterAttribute(string feature, string featureFilterName, object[] configs)
        {
            this.Feature = feature;
            this.FeatureFilterName = featureFilterName;
            this.Configs = configs;
        }

        public string Feature { get; }

        public string FeatureFilterName { get; }

        public object[] Configs { get; }
    }
}