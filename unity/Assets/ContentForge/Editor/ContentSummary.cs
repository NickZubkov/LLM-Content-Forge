namespace ContentForge.Editor
{
    /// <summary>Formats a generated SO's balance fields into a compact one-line preview summary.
    /// Pure — used by the window and unit-tested.</summary>
    internal static class ContentSummary
    {
        public static string Describe(ItemDefinition item) =>
            $"{item.rarity} · power {item.power} · value {item.value}";

        public static string Describe(EnemyDefinition enemy) =>
            $"level {enemy.level} · hp {enemy.health} · dmg {enemy.damage}";
    }
}
