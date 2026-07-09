namespace ContentForge.Api.Llm;

/// <summary>
/// Abstraction over an LLM backend. Returns the raw completion text; parsing and validation
/// live above this layer, which keeps the provider swappable behind a single seam.
/// </summary>
public interface ILlmClient
{
    Task<string> CompleteAsync(LlmPrompt prompt, CancellationToken cancellationToken);
}
