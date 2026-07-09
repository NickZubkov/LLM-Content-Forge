using System.Collections.Generic;
using Newtonsoft.Json;

namespace ContentForge.Editor
{
    /// <summary>Kinds of content the parser recognizes in a server response.</summary>
    internal enum GeneratedContentType
    {
        Item,
        Enemy
    }

    /// <summary>One item entry from <c>content[]</c>. Mirrors the server's camelCase JSON.</summary>
    internal sealed class GeneratedItemDto
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("description")] public string Description;
        [JsonProperty("rarity")] public string Rarity;
        [JsonProperty("power")] public int Power;
        [JsonProperty("value")] public int Value;
    }

    /// <summary>One enemy entry from <c>content[]</c>.</summary>
    internal sealed class GeneratedEnemyDto
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("description")] public string Description;
        [JsonProperty("level")] public int Level;
        [JsonProperty("health")] public int Health;
        [JsonProperty("damage")] public int Damage;
    }

    /// <summary>A parsed response. Exactly one of <see cref="Items"/> / <see cref="Enemies"/>
    /// is non-null, matching <see cref="ContentType"/>.</summary>
    internal sealed class ParsedGeneration
    {
        public GeneratedContentType ContentType;
        public IReadOnlyList<GeneratedItemDto> Items;
        public IReadOnlyList<GeneratedEnemyDto> Enemies;
    }
}
