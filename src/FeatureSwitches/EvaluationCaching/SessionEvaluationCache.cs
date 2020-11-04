using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;

namespace FeatureSwitches.EvaluationCaching
{
    /// <summary>
    /// A feature evalution cache. This should typically be registered as a scoped dependency.
    /// This cache ignores the sessionContextValue. This is on purpose and the reasoning is that it should always be the same.
    /// </summary>
    public class SessionEvaluationCache : IFeatureEvaluationCache
    {
        // There should be no need for locking / a concurrentdictionary when used as intended.
        private Dictionary<string, byte[]> state =
            new Dictionary<string, byte[]>();

        private bool loaded;

        public Task SetItem<T>(string feature, string sessionContextValue, T value)
        {
            if (!this.loaded)
            {
                this.state[feature] = JsonSerializer.SerializeToUtf8Bytes(value);
            }

            return Task.CompletedTask;
        }

        public Task<EvaluationCacheResult<T>?> GetItem<T>(string feature, string sessionContextValue)
        {
            if (this.state.TryGetValue(feature, out var bytes))
            {
                try
                {
                    var item = new EvaluationCacheResult<T> { };
                    item.Result = JsonSerializer.Deserialize<T>(bytes);
                    return Task.FromResult<EvaluationCacheResult<T>?>(item);
                }
                catch (JsonException)
                {
                }
            }

            return Task.FromResult<EvaluationCacheResult<T>?>(null);
        }

        public void ResetState()
        {
            this.loaded = false;
            this.state.Clear();
        }

        public byte[] GetState()
        {
            using var mso = new MemoryStream();
            using var deflate = new DeflateStream(mso, CompressionLevel.Optimal, false);
            using var utf8JsonWriter = new Utf8JsonWriter(deflate);
            JsonSerializer.Serialize(utf8JsonWriter, this.state);
            deflate.Flush();
            return mso.ToArray();
        }

        public void LoadState(byte[] state)
        {
            using var msi = new MemoryStream(state, false);
            using var deflate = new DeflateStream(msi, CompressionMode.Decompress, false);

            using var mso = new MemoryStream();
            deflate.CopyTo(mso);

            var uncompressedBytes = new ReadOnlySpan<byte>(mso.GetBuffer(), 0, (int)mso.Length);
            this.state = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(uncompressedBytes);
            this.loaded = true;
        }
    }
}
