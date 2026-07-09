using ContentForge.Api.Generation;
using ContentForge.Api.Llm;

namespace ContentForge.Api.Prompts;

/// <summary>Builds an <see cref="LlmPrompt"/> for a content type from caller parameters.</summary>
public interface IPromptBuilder
{
    LlmPrompt Build(ContentType contentType, GenerationParameters parameters);
}
