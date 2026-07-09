namespace ContentForge.Api.Llm;

/// <summary>Configuration for the LLM backend, bound from the "Llm" config section.</summary>
public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>Base URL of an OpenAI-compatible endpoint. Default targets a local Ollama.</summary>
    public string BaseUrl { get; set; } = "http://localhost:11434/v1/";

    /// <summary>Model identifier passed to the provider.</summary>
    public string Model { get; set; } = "qwen2.5:7b";

    public double Temperature { get; set; } = 0.7;

    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Optional bearer token. Provide via environment only (e.g. <c>Llm__ApiKey</c>); a local
    /// Ollama ignores it, but a cloud OpenAI-compatible provider would need it.
    /// </summary>
    public string? ApiKey { get; set; }
}
