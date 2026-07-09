using Newtonsoft.Json;

namespace ContentForge.Editor
{
    /// <summary>Request body sent to <c>POST /api/v1/generate</c>. Mirrors the server contract.</summary>
    internal sealed class GenerateRequestDto
    {
        [JsonProperty("contentType")] public string ContentType;
        [JsonProperty("count")] public int Count;
        [JsonProperty("theme")] public string Theme;
        [JsonProperty("levelRange")] public LevelRangeDto LevelRange;
    }

    internal sealed class LevelRangeDto
    {
        [JsonProperty("min")] public int Min;
        [JsonProperty("max")] public int Max;
    }
}
