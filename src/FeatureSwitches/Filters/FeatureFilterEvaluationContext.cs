using System.Text.Json;

namespace FeatureSwitches.Filters;

public sealed class FeatureFilterEvaluationContext
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly object? settings;

    public FeatureFilterEvaluationContext(string feature, object? settings)
    {
        this.Feature = feature;
        this.settings = settings;
    }

    public string Feature { get; }

    public T? GetSettings<T>()
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(this.settings), JsonSerializerOptions);
    }
}
