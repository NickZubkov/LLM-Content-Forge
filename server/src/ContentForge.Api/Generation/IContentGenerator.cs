namespace ContentForge.Api.Generation;

/// <summary>Orchestrates a full generation: prompt → LLM → parse → validate.</summary>
public interface IContentGenerator
{
    /// <summary>
    /// Generates and validates content for the request.
    /// </summary>
    /// <exception cref="ContentValidationException">The LLM output was unparsable or out of range.</exception>
    /// <exception cref="Llm.LlmException">The LLM backend was unreachable or errored.</exception>
    Task<GenerateResponse> GenerateAsync(GenerateRequest request, CancellationToken cancellationToken);
}
