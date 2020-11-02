using System.Text.Json;

namespace FeatureSwitches.Filters
{
    public class FeatureFilterEvaluationContext
    {
        private readonly byte[] settings;

        public FeatureFilterEvaluationContext(string feature, byte[] settings)
        {
            this.Feature = feature;
            this.settings = settings;
        }

        public string Feature { get; }

        public T GetSettings<T>()
        {
            return JsonSerializer.Deserialize<T>(this.settings);
        }
    }
}
