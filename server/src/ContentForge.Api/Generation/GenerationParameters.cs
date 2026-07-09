namespace ContentForge.Api.Generation;

/// <summary>An inclusive integer range, e.g. a balance band like level 1..10.</summary>
public readonly record struct IntRange(int Min, int Max);

/// <summary>Caller-supplied knobs for a generation request.</summary>
/// <param name="Count">How many entries to generate.</param>
/// <param name="Theme">Free-text theme, e.g. "frozen dungeon".</param>
/// <param name="LevelRange">Balance band the generated content should target.</param>
public sealed record GenerationParameters(int Count, string Theme, IntRange LevelRange);
