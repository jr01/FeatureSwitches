using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using FeatureSwitches.Definitions;

[assembly: CLSCompliant(true)]

namespace FeatureSwitches.MSTest;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class FeatureTestMethodAttribute : TestMethodAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureTestMethodAttribute"/> class.
    /// </summary>
    /// <param name="onOff">A comma separated string of features to vary between on/off.</param>
    /// <param name="on">A comma separated string of features that are always on.</param>
    /// <param name="off">A comma separated string of features that are always off.</param>
    /// <param name="displayName">The display name.</param>
    public FeatureTestMethodAttribute(
        string? onOff,
        string? on = null,
        string? off = null,
        string? displayName = null,
#pragma warning disable CA1019 // Define accessors for attribute arguments
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
#pragma warning disable SA1611 // Element parameters should be documented
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = -1)
#pragma warning restore SA1611 // Element parameters should be documented
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
#pragma warning restore CA1019 // Define accessors for attribute arguments
        : base(callerFilePath, callerLineNumber)
    {
        this.OnOff = onOff;
        this.On = on;
        this.Off = off;
        this.DisplayName = displayName;
    }

    /// <summary>
    /// Gets a comma separated string of features to vary between on/off.
    /// </summary>
    public string? OnOff { get; }

    /// <summary>
    /// Gets a comma separated string of features that are always on.
    /// </summary>
    public string? On { get; }

    /// <summary>
    /// Gets a comma separated string of features that are always off.
    /// </summary>
    public string? Off { get; }

    [CLSCompliant(false)]
    public static IReadOnlyList<FeatureDefinition> GetFeatures(TestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (FeatureTestMethodAttributeHelpers.ExecutingTestFeatures.TryGetValue(context.FullyQualifiedTestClassName + '/' + context.TestName, out var features))
        {
            return features;
        }

        return [];
    }

    public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
    {
        ArgumentNullException.ThrowIfNull(testMethod);

        static string[] Convert(string? arg)
        {
            return arg?.Split(FeatureTestMethodAttributeHelpers.ArgumentSeparator, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray() ?? [];
        }

        var on = Convert(this.On);
        var off = Convert(this.Off);
        var onOff = Convert(this.OnOff);

        var results = new List<TestResult>();
        var featuresTestValues = testMethod.GetAttributes<FeatureTestValueAttribute>();
        var allFeatures = on.Concat(off).Concat(onOff);

        var onCombinations = Enumerable.Range(0, 1 << onOff.Length)
            .Select(index => onOff.Where((v, i) => (index & (1 << i)) != 0).ToArray());
        foreach (var onCombination in onCombinations)
        {
            var outsideOnCombination = allFeatures.Except(onCombination);

            IEnumerable<IEnumerable<FeatureDefinition>> onCombinationValues = new List<List<FeatureDefinition>>
                {
                    new(),
                };

            foreach (var feature in onCombination)
            {
                var featureTestData = featuresTestValues.SingleOrDefault(x => x.Feature == feature);
                var onValues = featureTestData?.OnValues ?? [true];
                var offValue = featureTestData?.OffValue ?? false;

                onCombinationValues = onCombinationValues.SelectMany(o =>
                    onValues.Select(onValue =>
                        o.Concat(
                        [
                                new()
                                {
                                    Name = feature,
                                    IsOn = true,
                                    OffValue = offValue,
                                    OnValue = onValue,
                                },
                        ])));
            }

            foreach (var onCombinationValue in onCombinationValues)
            {
                var featureDefinitions = new List<FeatureDefinition>(onCombinationValue);

                foreach (var feature in outsideOnCombination)
                {
                    var featureTestData = featuresTestValues.SingleOrDefault(x => x.Feature == feature);
                    var onValue = featureTestData?.OnValues.FirstOrDefault() ?? true;
                    var offValue = featureTestData?.OffValue ?? false;

                    featureDefinitions.Add(new FeatureDefinition
                    {
                        Name = feature,
                        IsOn = on.Contains(feature),
                        OffValue = offValue,
                        OnValue = onValue,
                    });
                }

                var fullMethodName = testMethod.TestClassName + '/' + testMethod.TestMethodName;
                var sortedFeatures = featureDefinitions.OrderBy(x => x.Name).ToList();
                FeatureTestMethodAttributeHelpers.ExecutingTestFeatures.TryAdd(fullMethodName, sortedFeatures);

                var result = await testMethod.InvokeAsync(null).ConfigureAwait(false);

                static string GetOnOffValue(object? value)
                {
                    return value switch
                    {
                        bool b => b ? "on" : "off",
                        _ => value?.ToString() ?? "null",
                    };
                }

                var featureSet = string.Join(", ", sortedFeatures.Select(x => $"{x.Name}: {(x.IsOn ? $"{GetOnOffValue(x.OnValue)}" : $"{GetOnOffValue(x.OffValue)}")}"));
                result.DisplayName = $"{this.DisplayName ?? testMethod.TestMethodName} ({featureSet})";

                results.Add(result);

                FeatureTestMethodAttributeHelpers.ExecutingTestFeatures.TryRemove(fullMethodName, out _);
            }
        }

        return [.. results];
    }

    // This internal static class is a workaround for the diagnostic warning:
    // " MSTEST0057: TestMethodAttribute derived class 'FeatureTestMethodAttribute' should add CallerFilePath and CallerLineNumber parameters to its constructor (https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0057 "
    // The diagnostic does not recognize that when adding static fields a static constructor is generated by the compiler,
    // and that static constructor can not have the CallerFilePath and CallerLineNumber parameters.
    internal static class FeatureTestMethodAttributeHelpers
    {
        internal static readonly char[] ArgumentSeparator = [','];
        internal static readonly ConcurrentDictionary<string, IReadOnlyList<FeatureDefinition>> ExecutingTestFeatures = new();
    }
}
