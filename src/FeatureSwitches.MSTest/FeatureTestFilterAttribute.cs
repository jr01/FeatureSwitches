namespace FeatureSwitches.MSTest;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class FeatureTestFilterAttribute : Attribute
{
#pragma warning disable CA1019 // Define accessors for attribute arguments
    public FeatureTestFilterAttribute(string feature, string featureFilterName, object arg)
#pragma warning restore CA1019 // Define accessors for attribute arguments
            : this(feature, featureFilterName, [arg])
    {
    }

#pragma warning disable CA1019 // Define accessors for attribute arguments
    public FeatureTestFilterAttribute(string feature, string featureFilterName, object arg1, object arg2)
#pragma warning restore CA1019 // Define accessors for attribute arguments
             : this(feature, featureFilterName, [arg1, arg2])
    {
    }

#pragma warning disable CA1019 // Define accessors for attribute arguments
    public FeatureTestFilterAttribute(string feature, string featureFilterName, object arg1, object arg2, object arg3)
#pragma warning restore CA1019 // Define accessors for attribute arguments
            : this(feature, featureFilterName, [arg1, arg2, arg3])
    {
    }

    [CLSCompliant(false)]
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
