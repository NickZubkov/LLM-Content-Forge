namespace ContentForge.Api.Generation;

/// <summary>A generated game enemy, as parsed from the LLM response.</summary>
public sealed record GeneratedEnemy(
    string Name,
    string Description,
    int Level,
    int Health,
    int Damage);
