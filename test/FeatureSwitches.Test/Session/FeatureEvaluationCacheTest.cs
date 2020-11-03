using System;
using FeatureSwitches.Definitions;
using FeatureSwitches.EvaluationCaching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureSwitches.Test.Session
{
    [TestClass]
    public class FeatureEvaluationCacheTest
    {
        [TestMethod]
        public void FeatureEvaluationCache_caches_using_evaluationcontext()
        {
            var provider = new TestProvider();
            using var cache = new FeatureEvaluationCache(provider);

            cache.AddOrUpdate("A", "C-1", true);
            cache.AddOrUpdate("A", "C-2", false);
            cache.AddOrUpdate("B", "C-1", false);

            Assert.IsTrue(cache.TryGetValue<bool>("A", "C-1", out var value));
            Assert.IsTrue(value);
            Assert.IsTrue(cache.TryGetValue<bool>("A", "C-2", out value));
            Assert.IsFalse(value);
            Assert.IsTrue(cache.TryGetValue<bool>("B", "C-1", out value));
            Assert.IsFalse(value);
            Assert.IsFalse(cache.TryGetValue<bool>("A", "C-3", out _));
        }

        [TestMethod]
        public void FeatureEvaluationCache_resets_when_feature_changes()
        {
            var provider = new TestProvider();
            using var cache = new FeatureEvaluationCache(provider);

            cache.AddOrUpdate("A", "C-1", true);
            cache.AddOrUpdate("A", "C-2", false);
            cache.AddOrUpdate("B", "C-1", false);

            provider.InvokeChange("A");

            Assert.IsFalse(cache.TryGetValue<bool>("A", "C-1", out _));
            Assert.IsFalse(cache.TryGetValue<bool>("A", "C-2", out _));
            Assert.IsTrue(cache.TryGetValue<bool>("B", "C-1", out var value));
            Assert.IsFalse(value);
            Assert.IsFalse(cache.TryGetValue<bool>("A", "C-3", out _));
        }

        [TestMethod]
        public void FeatureEvaluationCache_ignores_invalid_cast()
        {
            var provider = new TestProvider();
            using var cache = new FeatureEvaluationCache(provider);

            cache.AddOrUpdate("A", "C-1", true);

            Assert.IsFalse(cache.TryGetValue<string>("A", "C-1", out _));
        }

        [TestMethod]
        public void FeatureEvaluationCache_AddOrUpdate_updates_value()
        {
            var provider = new TestProvider();
            using var cache = new FeatureEvaluationCache(provider);

            cache.AddOrUpdate("A", "C-1", true);

            Assert.IsTrue(cache.TryGetValue<bool>("A", "C-1", out var value));
            Assert.IsTrue(value);

            cache.AddOrUpdate("A", "C-1", false);

            Assert.IsTrue(cache.TryGetValue<bool>("A", "C-1", out value));
            Assert.IsFalse(value);
        }

        private class TestProvider : IFeatureDefinitionProvider
        {
            public event EventHandler<FeatureDefinitionChangeEventArgs>? Changed;

            public void InvokeChange(string feature)
            {
                this.Changed?.Invoke(this, new FeatureDefinitionChangeEventArgs(feature));
            }

            public FeatureDefinition GetFeatureDefinition(string feature)
            {
                throw new NotImplementedException();
            }

            public string[] GetFeatures()
            {
                throw new NotImplementedException();
            }
        }
    }
}
