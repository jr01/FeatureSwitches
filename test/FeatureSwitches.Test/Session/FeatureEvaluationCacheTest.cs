using System;
using System.Threading.Tasks;
using FeatureSwitches.Definitions;
using FeatureSwitches.EvaluationCaching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureSwitches.Test.Session
{
    [TestClass]
    public class FeatureEvaluationCacheTest
    {
        [TestMethod]
        public async Task FeatureEvaluationCache_caches_using_evaluationcontext()
        {
            var provider = new TestProvider();
            using var cache = new FeatureEvaluationCache(provider);

            await cache.SetItem("A", "C-1", true);
            await cache.SetItem("A", "C-2", false);
            await cache.SetItem("B", "C-1", false);

            var item = await cache.GetItem<bool>("A", "C-1");
            Assert.IsNotNull(item);
            Assert.IsTrue(item!.Result);
            item = await cache.GetItem<bool>("A", "C-2");
            Assert.IsNotNull(item);
            Assert.IsFalse(item!.Result);
            item = await cache.GetItem<bool>("B", "C-1");
            Assert.IsNotNull(item);
            Assert.IsFalse(item!.Result);
            item = await cache.GetItem<bool>("A", "C-3");
            Assert.IsNull(item);
        }

        [TestMethod]
        public async Task FeatureEvaluationCache_resets_when_feature_changes()
        {
            var provider = new TestProvider();
            using var cache = new FeatureEvaluationCache(provider);

            await cache.SetItem("A", "C-1", true);
            await cache.SetItem("A", "C-2", false);
            await cache.SetItem("B", "C-1", false);

            provider.InvokeChange("A");

            var item = await cache.GetItem<bool>("A", "C-1");
            Assert.IsNull(item);
            item = await cache.GetItem<bool>("A", "C-2");
            Assert.IsNull(item);
            item = await cache.GetItem<bool>("B", "C-1");
            Assert.IsNotNull(item);
            Assert.IsFalse(item!.Result);
            item = await cache.GetItem<bool>("A", "C-3");
            Assert.IsNull(item);
        }

        [TestMethod]
        public async Task FeatureEvaluationCache_ignores_invalid_cast()
        {
            var provider = new TestProvider();
            using var cache = new FeatureEvaluationCache(provider);

            await cache.SetItem("A", "C-1", true);

            var item = await cache.GetItem<string>("A", "C-1");
            Assert.IsNull(item);
        }

        [TestMethod]
        public async Task FeatureEvaluationCache_AddOrUpdate_updates_value()
        {
            var provider = new TestProvider();
            using var cache = new FeatureEvaluationCache(provider);

            await cache.SetItem("A", "C-1", true);

            var item = await cache.GetItem<bool>("A", "C-1");
            Assert.IsNotNull(item);
            Assert.IsTrue(item!.Result);

            await cache.SetItem("A", "C-1", false);

            item = await cache.GetItem<bool>("A", "C-1");
            Assert.IsNotNull(item);
            Assert.IsFalse(item!.Result);
        }

        private class TestProvider : IFeatureDefinitionProvider
        {
            public event EventHandler<FeatureDefinitionChangeEventArgs>? Changed;

            public void InvokeChange(string feature)
            {
                this.Changed?.Invoke(this, new FeatureDefinitionChangeEventArgs(feature));
            }

            public Task<FeatureDefinition?> GetFeatureDefinition(string feature)
            {
                throw new NotImplementedException();
            }

            public Task<string[]> GetFeatures()
            {
                throw new NotImplementedException();
            }
        }
    }
}
