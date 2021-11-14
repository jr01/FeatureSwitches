namespace FeatureSwitches.Caching;

public interface IFeatureCache
{
    Task<byte[]?> GetItem(string feature, string context, CancellationToken cancellationToken = default);

    Task SetItem(string feature, string context, byte[] value, FeatureCacheOptions? options = null, CancellationToken cancellationToken = default);

    Task Remove(string feature, CancellationToken cancellationToken = default);
}
