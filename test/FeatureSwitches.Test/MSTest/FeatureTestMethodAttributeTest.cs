using System.Reflection;
using FeatureSwitches.Definitions;
using FeatureSwitches.MSTest;

namespace FeatureSwitches.Test.MSTest;

[TestClass]
public sealed class FeatureTestMethodAttributeTest
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void Single_feature_on_off()
    {
        var attr = new FeatureTestMethodAttribute(onOff: "A");

        var testMethod = new MockTestMethod(this.TestContext);
        var testResults = attr.Execute(testMethod);

        Assert.AreEqual(2, testMethod.Executions.Count);

        Assert.AreEqual(1, testMethod.Executions[0].Features.Count);
        Assert.AreEqual("A", testMethod.Executions[0].Features[0].Name);
        Assert.AreEqual(false, testMethod.Executions[0].Features[0].IsOn);
        Assert.AreEqual(true, testMethod.Executions[0].Features[0].OnValue);
        Assert.AreEqual(false, testMethod.Executions[0].Features[0].OffValue);

        Assert.AreEqual(1, testMethod.Executions[1].Features.Count);
        Assert.AreEqual("A", testMethod.Executions[1].Features[0].Name);
        Assert.AreEqual(true, testMethod.Executions[1].Features[0].IsOn);
        Assert.AreEqual(true, testMethod.Executions[1].Features[0].OnValue);
        Assert.AreEqual(false, testMethod.Executions[1].Features[0].OffValue);

        Assert.AreEqual(2, testResults.Length);
        Assert.AreEqual($"{nameof(this.Single_feature_on_off)} (A: off)", testResults[0].DisplayName);
        Assert.AreEqual($"{nameof(this.Single_feature_on_off)} (A: on)", testResults[1].DisplayName);
    }

    [TestMethod]
    public void Two_features_on_off()
    {
        var attr = new FeatureTestMethodAttribute(onOff: "A,B");

        var testMethod = new MockTestMethod(this.TestContext);
        var testResults = attr.Execute(testMethod);

        Assert.AreEqual(4, testMethod.Executions.Count);

        Assert.AreEqual(2, testMethod.Executions[0].Features.Count);
        Assert.AreEqual("A", testMethod.Executions[0].Features[0].Name);
        Assert.AreEqual(false, testMethod.Executions[0].Features[0].IsOn);
        Assert.AreEqual(true, testMethod.Executions[0].Features[0].OnValue);
        Assert.AreEqual(false, testMethod.Executions[0].Features[0].OffValue);
        Assert.AreEqual("B", testMethod.Executions[0].Features[1].Name);
        Assert.AreEqual(false, testMethod.Executions[0].Features[1].IsOn);
        Assert.AreEqual(true, testMethod.Executions[0].Features[1].OnValue);
        Assert.AreEqual(false, testMethod.Executions[0].Features[1].OffValue);

        Assert.AreEqual(2, testMethod.Executions[1].Features.Count);
        Assert.AreEqual("A", testMethod.Executions[1].Features[0].Name);
        Assert.AreEqual(true, testMethod.Executions[1].Features[0].IsOn);
        Assert.AreEqual("B", testMethod.Executions[1].Features[1].Name);
        Assert.AreEqual(false, testMethod.Executions[1].Features[1].IsOn);

        Assert.AreEqual(2, testMethod.Executions[2].Features.Count);
        Assert.AreEqual("A", testMethod.Executions[2].Features[0].Name);
        Assert.AreEqual(false, testMethod.Executions[2].Features[0].IsOn);
        Assert.AreEqual("B", testMethod.Executions[2].Features[1].Name);
        Assert.AreEqual(true, testMethod.Executions[2].Features[1].IsOn);

        Assert.AreEqual(2, testMethod.Executions[3].Features.Count);
        Assert.AreEqual("A", testMethod.Executions[3].Features[0].Name);
        Assert.AreEqual(true, testMethod.Executions[3].Features[0].IsOn);
        Assert.AreEqual("B", testMethod.Executions[3].Features[1].Name);
        Assert.AreEqual(true, testMethod.Executions[3].Features[1].IsOn);

        Assert.AreEqual(4, testResults.Length);
        Assert.AreEqual($"{nameof(this.Two_features_on_off)} (A: off, B: off)", testResults[0].DisplayName);
        Assert.AreEqual($"{nameof(this.Two_features_on_off)} (A: on, B: off)", testResults[1].DisplayName);
        Assert.AreEqual($"{nameof(this.Two_features_on_off)} (A: off, B: on)", testResults[2].DisplayName);
        Assert.AreEqual($"{nameof(this.Two_features_on_off)} (A: on, B: on)", testResults[3].DisplayName);
    }

    [TestMethod]
    public void Single_feature_on_off_and_feature_on_and_feature_off()
    {
        var attr = new FeatureTestMethodAttribute(onOff: "A", on: "B", off: "C");

        var testMethod = new MockTestMethod(this.TestContext);
        var testResults = attr.Execute(testMethod);

        Assert.AreEqual(2, testMethod.Executions.Count);

        Assert.AreEqual(3, testMethod.Executions[0].Features.Count);
        Assert.AreEqual("A", testMethod.Executions[0].Features[0].Name);
        Assert.AreEqual(false, testMethod.Executions[0].Features[0].IsOn);
        Assert.AreEqual("B", testMethod.Executions[0].Features[1].Name);
        Assert.AreEqual(true, testMethod.Executions[0].Features[1].IsOn);
        Assert.AreEqual("C", testMethod.Executions[0].Features[2].Name);
        Assert.AreEqual(false, testMethod.Executions[0].Features[2].IsOn);

        Assert.AreEqual(3, testMethod.Executions[1].Features.Count);
        Assert.AreEqual("A", testMethod.Executions[1].Features[0].Name);
        Assert.AreEqual(true, testMethod.Executions[1].Features[0].IsOn);
        Assert.AreEqual("B", testMethod.Executions[1].Features[1].Name);
        Assert.AreEqual(true, testMethod.Executions[1].Features[1].IsOn);
        Assert.AreEqual("C", testMethod.Executions[1].Features[2].Name);
        Assert.AreEqual(false, testMethod.Executions[1].Features[2].IsOn);

        Assert.AreEqual(2, testResults.Length);
        Assert.AreEqual($"{nameof(this.Single_feature_on_off_and_feature_on_and_feature_off)} (A: off, B: on, C: off)", testResults[0].DisplayName);
        Assert.AreEqual($"{nameof(this.Single_feature_on_off_and_feature_on_and_feature_off)} (A: on, B: on, C: off)", testResults[1].DisplayName);
    }

    [TestMethod]
    public void Single_feature_on_off_typed()
    {
        var attr = new FeatureTestMethodAttribute(onOff: "A");

        var testData = new FeatureTestValueAttribute("A", onValue: "High", offValue: "Low");

        var testMethod = new MockTestMethod(this.TestContext, [testData]);
        var testResults = attr.Execute(testMethod);

        Assert.AreEqual(2, testMethod.Executions.Count);

        Assert.AreEqual("A", testMethod.Executions[0].Features[0].Name);
        Assert.AreEqual(false, testMethod.Executions[0].Features[0].IsOn);
        Assert.AreEqual("High", testMethod.Executions[0].Features[0].OnValue);
        Assert.AreEqual("Low", testMethod.Executions[0].Features[0].OffValue);

        Assert.AreEqual(2, testResults.Length);
        Assert.AreEqual($"{nameof(this.Single_feature_on_off_typed)} (A: Low)", testResults[0].DisplayName);
        Assert.AreEqual($"{nameof(this.Single_feature_on_off_typed)} (A: High)", testResults[1].DisplayName);
    }

    [TestMethod]
    public void Single_feature_on_off_typed_multiple_onvalues()
    {
        var attr = new FeatureTestMethodAttribute(onOff: "A");

        var testData = new FeatureTestValueAttribute("A", onValues: ["On1", "On2"], offValue: "Off");

        var testMethod = new MockTestMethod(this.TestContext, [testData]);
        var testResults = attr.Execute(testMethod);

        Assert.AreEqual(3, testMethod.Executions.Count);

        Assert.AreEqual(false, testMethod.Executions[0].Features[0].IsOn);
        Assert.AreEqual("On1", testMethod.Executions[0].Features[0].OnValue);
        Assert.AreEqual("Off", testMethod.Executions[0].Features[0].OffValue);

        Assert.AreEqual(true, testMethod.Executions[1].Features[0].IsOn);
        Assert.AreEqual("On1", testMethod.Executions[1].Features[0].OnValue);
        Assert.AreEqual("Off", testMethod.Executions[1].Features[0].OffValue);

        Assert.AreEqual(true, testMethod.Executions[2].Features[0].IsOn);
        Assert.AreEqual("On2", testMethod.Executions[2].Features[0].OnValue);
        Assert.AreEqual("Off", testMethod.Executions[2].Features[0].OffValue);

        Assert.AreEqual(3, testResults.Length);
        Assert.AreEqual($"{nameof(this.Single_feature_on_off_typed_multiple_onvalues)} (A: Off)", testResults[0].DisplayName);
        Assert.AreEqual($"{nameof(this.Single_feature_on_off_typed_multiple_onvalues)} (A: On1)", testResults[1].DisplayName);
        Assert.AreEqual($"{nameof(this.Single_feature_on_off_typed_multiple_onvalues)} (A: On2)", testResults[2].DisplayName);
    }

    public sealed class MockTestMethod : ITestMethod
    {
        private readonly TestContext testContext;
        private readonly IList<Attribute> testAttributes;

        public MockTestMethod(TestContext testContext)
            : this(testContext, [])
        {
        }

        public MockTestMethod(TestContext testContext, IList<Attribute> testAttributes)
        {
            this.testContext = testContext;
            this.testAttributes = testAttributes;
        }

        public IList<MockTestMethodExecution> Executions { get; } = [];

        public string TestMethodName => this.testContext.TestName!;

        public string TestClassName => this.testContext.FullyQualifiedTestClassName!;

        public Type ReturnType => throw new InvalidOperationException();

        public object[] Arguments => throw new InvalidOperationException();

        public ParameterInfo[] ParameterTypes => throw new InvalidOperationException();

        public MethodInfo MethodInfo => throw new InvalidOperationException();

        public Attribute[] GetAllAttributes(bool inherit)
        {
            throw new InvalidOperationException();
        }

        public TAttributeType[] GetAttributes<TAttributeType>(bool inherit)
            where TAttributeType : Attribute
        {
            return this.testAttributes.OfType<TAttributeType>().ToArray();
        }

        public TestResult Invoke(object[]? arguments)
        {
            this.Executions.Add(new() { Features = FeatureTestMethodAttribute.GetFeatures(this.testContext) });

            return new TestResult();
        }

        public sealed class MockTestMethodExecution
        {
            public IReadOnlyList<FeatureDefinition> Features { get; set; } = default!;
        }
    }
}
