using System;

namespace FeatureSwitches.MSTest
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class FeatureTestValueAttribute : Attribute
    {
        public FeatureTestValueAttribute(string feature)
            : this(feature, offValue: false, onValue: true)
        {
        }

        public FeatureTestValueAttribute(string feature, object offValue, object onValue)
            : this(feature, offValue: offValue, onValues: onValue == null ? Array.Empty<object>() : new object[] { onValue })
        {
        }

        public FeatureTestValueAttribute(string feature, object offValue, object[] onValues)
        {
            this.Feature = feature;
            this.OffValue = offValue;
            this.OnValues = onValues;
        }

        public string Feature { get; private set; }

        public object OffValue { get; private set; }

        public object[] OnValues { get; private set; }
    }
}