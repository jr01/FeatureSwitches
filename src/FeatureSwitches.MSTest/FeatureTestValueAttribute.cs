namespace FeatureSwitches.MSTest;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class FeatureTestValueAttribute : Attribute
{
    public FeatureTestValueAttribute(string feature)
        : this(feature, offValue: false, onValue: true)
    {
    }

#pragma warning disable CA1019 // Define accessors for attribute arguments
    public FeatureTestValueAttribute(string feature, object offValue, object? onValue)
#pragma warning restore CA1019 // Define accessors for attribute arguments
            : this(feature, offValue: offValue, onValues: onValue == null ? [] : [onValue])
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
