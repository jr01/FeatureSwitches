using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FeatureSwitches.Definitions;
using FeatureSwitches.Filters;
using FeatureSwitches.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureSwitches.Test.MSTest
{
    [TestClass]
    public class FeatureTestMethodAttributeTest
    {
        [TestMethod]
        public void Single_feature_on_off()
        {
            var attr = new FeatureTestMethodAttribute(onOff: "A");

            var testMethod = new MockTestMethod();
            attr.Execute(testMethod);

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
        }

        [TestMethod]
        public void Two_features_on_off()
        {
            var attr = new FeatureTestMethodAttribute(onOff: "A,B");

            var testMethod = new MockTestMethod();
            attr.Execute(testMethod);

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
        }

        [TestMethod]
        public void Single_feature_on_off_and_feature_on_and_feature_off()
        {
            var attr = new FeatureTestMethodAttribute(onOff: "A", on: "B", off: "C");

            var testMethod = new MockTestMethod();
            attr.Execute(testMethod);

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
        }

        [TestMethod]
        public void Single_feature_on_off_typed()
        {
            var attr = new FeatureTestMethodAttribute(onOff: "A");

            var testData = new FeatureTestValueAttribute("A", onValue: "On", offValue: "Off");

            var testMethod = new MockTestMethod(new () { testData });
            attr.Execute(testMethod);

            Assert.AreEqual(2, testMethod.Executions.Count);

            Assert.AreEqual("A", testMethod.Executions[0].Features[0].Name);
            Assert.AreEqual(false, testMethod.Executions[0].Features[0].IsOn);
            Assert.AreEqual("On", testMethod.Executions[0].Features[0].OnValue);
            Assert.AreEqual("Off", testMethod.Executions[0].Features[0].OffValue);
        }

        [TestMethod]
        public void Single_feature_on_off_typed_multiple_onvalues()
        {
            var attr = new FeatureTestMethodAttribute(onOff: "A");

            var testData = new FeatureTestValueAttribute("A", onValues: new object[] { "On1", "On2" }, offValue: "Off");

            var testMethod = new MockTestMethod(new () { testData });
            attr.Execute(testMethod);

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
        }

        public class MockTestMethod : ITestMethod
        {
            private readonly List<Attribute> testAttributes;

            public MockTestMethod()
                : this(new ())
            {
            }

            public MockTestMethod(List<Attribute> testAttributes)
            {
                this.testAttributes = testAttributes;
            }

            public List<MockTestMethodExecution> Executions { get; } = new ();

            public string TestMethodName => throw new InvalidOperationException();

            public string TestClassName => throw new InvalidOperationException();

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

            public TestResult Invoke(object[] arguments)
            {
                this.Executions.Add(new () { Features = FeatureTestMethodAttribute.Features.ToList() });

                return new TestResult();
            }

            public class MockTestMethodExecution
            {
                public IList<FeatureDefinition> Features { get; set; } = default!;
            }
        }
    }
}