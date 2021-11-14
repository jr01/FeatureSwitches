using System.Text.Json;
using FeatureSwitches.Caching;

namespace FeatureSwitches.Test.Caching;

[TestClass]
public class InMemoryEvaluationCacheTest
{
    [TestMethod]
    public async Task FeatureEvaluationCache_caches_using_evaluationcontext()
    {
        var cache = new InMemoryFeatureCache();

        await cache.SetItem("A", "C-1", JsonSerializer.SerializeToUtf8Bytes(true));
        await cache.SetItem("A", "C-2", JsonSerializer.SerializeToUtf8Bytes(false));
        await cache.SetItem("B", "C-1", JsonSerializer.SerializeToUtf8Bytes(false));

        var item = await cache.GetItem("A", "C-1");
        Assert.IsNotNull(item);
        Assert.IsTrue(JsonSerializer.Deserialize<bool>(item));
        item = await cache.GetItem("A", "C-2");
        Assert.IsNotNull(item);
        Assert.IsFalse(JsonSerializer.Deserialize<bool>(item));
        item = await cache.GetItem("B", "C-1");
        Assert.IsNotNull(item);
        Assert.IsFalse(JsonSerializer.Deserialize<bool>(item));
        item = await cache.GetItem("A", "C-3");
        Assert.IsNull(item);
    }

    [TestMethod]
    public async Task FeatureEvaluationCache_resets_when_feature_changes()
    {
        var cache = new InMemoryFeatureCache();

        await cache.SetItem("A", "C-1", JsonSerializer.SerializeToUtf8Bytes(true));
        await cache.SetItem("A", "C-2", JsonSerializer.SerializeToUtf8Bytes(false));
        await cache.SetItem("B", "C-1", JsonSerializer.SerializeToUtf8Bytes(false));

        await cache.Remove("A");

        var item = await cache.GetItem("A", "C-1");
        Assert.IsNull(item);
        item = await cache.GetItem("A", "C-2");
        Assert.IsNull(item);
        item = await cache.GetItem("B", "C-1");
        Assert.IsNotNull(item);
        Assert.IsFalse(JsonSerializer.Deserialize<bool>(item));
        item = await cache.GetItem("A", "C-3");
        Assert.IsNull(item);
    }

    [TestMethod]
    public async Task FeatureEvaluationCache_AddOrUpdate_updates_value()
    {
        var cache = new InMemoryFeatureCache();

        await cache.SetItem("A", "C-1", JsonSerializer.SerializeToUtf8Bytes(true));

        var item = await cache.GetItem("A", "C-1");
        Assert.IsNotNull(item);
        Assert.IsTrue(JsonSerializer.Deserialize<bool>(item));

        await cache.SetItem("A", "C-1", JsonSerializer.SerializeToUtf8Bytes(false));

        item = await cache.GetItem("A", "C-1");
        Assert.IsNotNull(item);
        Assert.IsFalse(JsonSerializer.Deserialize<bool>(item));
    }
}
