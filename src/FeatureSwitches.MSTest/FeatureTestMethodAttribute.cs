using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FeatureSwitches.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureSwitches.MSTest
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FeatureTestMethodAttribute : TestMethodAttribute
    {
        private static readonly AsyncLocal<IEnumerable<FeatureDefinition>> AsyncLocalFeatures = new AsyncLocal<IEnumerable<FeatureDefinition>>();

        private readonly string[] onOff;

        private readonly string[] on;

        private readonly string[] off;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureTestMethodAttribute"/> class.
        /// </summary>
        /// <param name="onOff">A comma separated string of features to vary between on/off.</param>
        /// <param name="on">A comma separated string of features that are always on.</param>
        /// <param name="off">A comma separated string of features that are always off.</param>
        public FeatureTestMethodAttribute(string? onOff, string? on = null, string? off = null)
        {
            static string[] Convert(string? arg)
            {
                return arg?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray() ?? Array.Empty<string>();
            }

            this.onOff = Convert(onOff);
            this.on = Convert(on);
            this.off = Convert(off);
        }

        public static IEnumerable<FeatureDefinition> Features => AsyncLocalFeatures.Value ?? Array.Empty<FeatureDefinition>();

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            var results = new List<TestResult>();
            var featuresTestValues = testMethod.GetAttributes<FeatureTestValueAttribute>(false);
            var allFeatures = this.on.Concat(this.off).Concat(this.onOff);

            var onCombinations = Enumerable.Range(0, 1 << this.onOff.Length)
                .Select(index => this.onOff.Where((v, i) => (index & (1 << i)) != 0).ToArray());
            foreach (var onCombination in onCombinations)
            {
                var outsideOnCombination = allFeatures.Except(onCombination);

                IEnumerable<IEnumerable<FeatureDefinition>> onCombinationValues = new List<List<FeatureDefinition>>
                {
                    new List<FeatureDefinition>()
                };

                foreach (var feature in onCombination)
                {
                    var featureTestData = featuresTestValues.SingleOrDefault(x => x.Feature == feature);
                    var onValues = featureTestData?.OnValues ?? new object[] { true };
                    var offValue = featureTestData?.OffValue ?? false;

                    onCombinationValues = onCombinationValues.SelectMany(o =>
                        onValues.Select(onValue =>
                            o.Concat(new List<FeatureDefinition>
                            {
                                new FeatureDefinition
                                {
                                    Name = feature,
                                    IsOn = true,
                                    OffValue = offValue,
                                    OnValue = onValue
                                }
                            })));
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
                            IsOn = this.on.Contains(feature),
                            OffValue = offValue,
                            OnValue = onValue
                        });
                    }

                    AsyncLocalFeatures.Value = featureDefinitions.OrderBy(x => x.Name);

                    var result = testMethod.Invoke(null);
                    results.Add(result);
                }
            }

            return results.ToArray();
        }
    }
}