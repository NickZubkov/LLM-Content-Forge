namespace ContentForge.Api.Llm;

/// <summary>Raised when the LLM backend call fails or returns an unusable response.</summary>
public sealed class LlmException : Exception
{
    public LlmException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
