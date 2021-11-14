using System.Text.Json.Serialization;

namespace FeatureSwitches.Test.IntegrationTest;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MultiSwitch
{
    /// <summary>
    /// Switch is On.
    /// </summary>
    On,

    /// <summary>
    /// Switch is halfway.
    /// </summary>
    Halfway,

    /// <summary>
    /// Switch is off.
    /// </summary>
    Off
}
