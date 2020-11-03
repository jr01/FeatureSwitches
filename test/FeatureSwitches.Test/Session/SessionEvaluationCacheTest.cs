using FeatureSwitches.EvaluationCaching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureSwitches.Test.Session
{
    [TestClass]
    public class SessionEvaluationCacheTest
    {
        [TestMethod]
        public void Save_and_load()
        {
            var sessionCache = new SessionEvaluationCache();
            sessionCache.AddOrUpdate("featureA", string.Empty, true);
            sessionCache.AddOrUpdate("featureB", string.Empty, false);
            sessionCache.AddOrUpdate("featureC", string.Empty, ABTest.B);

            var state = sessionCache.GetState();

            sessionCache.LoadState(state);

            if (!sessionCache.TryGetValue<bool>("featureA", string.Empty, out var boolValue))
            {
                Assert.Fail();
            }

            Assert.IsTrue(boolValue);

            if (!sessionCache.TryGetValue<bool>("featureB", string.Empty, out boolValue))
            {
                Assert.Fail();
            }

            Assert.IsFalse(boolValue);

            if (!sessionCache.TryGetValue<ABTest>("featureC", string.Empty, out var enumValue))
            {
                Assert.Fail();
            }

            Assert.AreEqual(ABTest.B, enumValue);
        }

        [TestMethod]
        public void Sessioncache_speed()
        {
            var sessionCache = new SessionEvaluationCache();
            const int MaxFeatures = 10000;
            for (int i = 0; i < MaxFeatures; i++)
            {
                sessionCache.AddOrUpdate($"feature{i}", string.Empty, true);
            }

            var state = sessionCache.GetState();

            sessionCache.LoadState(state);

            for (int i = 0; i < MaxFeatures; i++)
            {
                if (!sessionCache.TryGetValue<bool>($"feature{i}", string.Empty, out var boolValue))
                {
                    Assert.Fail();
                }

                Assert.IsTrue(boolValue);
            }
        }

        private class Person
        {
            public string Name { get; set; } = null!;
        }
    }
}
