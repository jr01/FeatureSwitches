//using System.Threading.Tasks;
//using FeatureSwitches.Caching;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//namespace FeatureSwitches.Test.Session
//{
//    [TestClass]
//    public sealed class SessionEvaluationCacheTest
//    {
//        [TestMethod]
//        public async Task Save_and_load()
//        {
//            var sessionCache = new SessionEvaluationCache();
//            await sessionCache.SetItem("featureA", string.Empty, true);
//            await sessionCache.SetItem("featureB", string.Empty, false);
//            await sessionCache.SetItem("featureC", string.Empty, ABTest.B);

//            var state = sessionCache.GetState();

//            sessionCache.LoadState(state);

//            var item = await sessionCache.GetItem<bool>("featureA", string.Empty);
//            Assert.IsNotNull(item);
//            Assert.IsTrue(item!.Result);

//            item = await sessionCache.GetItem<bool>("featureB", string.Empty);
//            Assert.IsNotNull(item);
//            Assert.IsFalse(item!.Result);

//            var enumItem = await sessionCache.GetItem<ABTest>("featureC", string.Empty);
//            Assert.IsNotNull(enumItem);
//            Assert.AreEqual(ABTest.B, enumItem!.Result);
//        }

//        [TestMethod]
//        public async Task Sessioncache_speed()
//        {
//            var sessionCache = new SessionEvaluationCache();
//            const int MaxFeatures = 10000;
//            for (int i = 0; i < MaxFeatures; i++)
//            {
//                await sessionCache.SetItem($"feature{i}", string.Empty, true);
//            }

//            var state = sessionCache.GetState();

//            sessionCache.LoadState(state);

//            for (int i = 0; i < MaxFeatures; i++)
//            {
//                var item = await sessionCache.GetItem<bool>($"feature{i}", string.Empty);
//                Assert.IsNotNull(item);
//                Assert.IsTrue(item!.Result);
//            }
//        }

//        private sealed class Person
//        {
//            public string Name { get; set; } = null!;
//        }
//    }
//}
