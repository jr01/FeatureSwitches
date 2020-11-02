using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

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

        public void AddOrUpdate<T>(string feature, string sessionContextValue, T value)
        {
            if (!this.loaded)
            {
                this.state[feature] = JsonSerializer.SerializeToUtf8Bytes(value);
            }
        }

        public bool TryGetValue<T>(string feature, string sessionContextValue, out T value)
        {
            if (this.state.TryGetValue(feature, out var bytes))
            {
                try
                {
                    value = JsonSerializer.Deserialize<T>(bytes);
                    return true;
                }
                catch (JsonException)
                {
                }

                value = default!;
                return false;
            }

            value = default!;
            return false;
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
