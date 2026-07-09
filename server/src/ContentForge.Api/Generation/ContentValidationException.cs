namespace ContentForge.Api.Generation;

/// <summary>
/// Raised when the LLM response cannot be parsed into the requested content type, or when a
/// generated entry violates the requested structure or value ranges.
/// </summary>
public sealed class ContentValidationException : Exception
{
    public ContentValidationException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
