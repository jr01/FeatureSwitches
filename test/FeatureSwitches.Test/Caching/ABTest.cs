using System.Text.Json.Serialization;

namespace FeatureSwitches.Test.Session
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ABTest
    {
        /// <summary>
        /// Choose A.
        /// </summary>
        A,

        /// <summary>
        /// Choose B.
        /// </summary>
        B
    }
}
