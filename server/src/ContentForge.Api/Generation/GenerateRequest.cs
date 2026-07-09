namespace ContentForge.Api.Generation;

/// <summary>Request body for <c>POST /api/v1/generate</c>.</summary>
/// <param name="ContentType">Which kind of content to generate.</param>
/// <param name="Count">How many entries to generate.</param>
/// <param name="Theme">Free-text theme, e.g. "frozen dungeon".</param>
/// <param name="LevelRange">Balance band the generated content must fall within.</param>
public sealed record GenerateRequest(
    ContentType ContentType,
    int Count,
    string Theme,
    IntRange LevelRange);
