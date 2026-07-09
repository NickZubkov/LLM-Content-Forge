namespace ContentForge.Api.Generation;

/// <summary>A generated game item, as parsed from the LLM response.</summary>
public sealed record GeneratedItem(
    string Name,
    string Description,
    string Rarity,
    int Power,
    int Value);
