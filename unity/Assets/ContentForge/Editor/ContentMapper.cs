using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentForge.Editor
{
    /// <summary>A DTO mapped to an in-memory SO, plus the outcome of client-side validation.
    /// The SO is not yet an asset.</summary>
    internal sealed class Mapped<T> where T : ScriptableObject
    {
        public string Slug;
        public string SourceName;
        public T Value;
        public IReadOnlyList<string> Errors;
        public bool IsValid => Errors.Count == 0;
    }

    /// <summary>Maps parsed DTOs into populated SO instances and applies a second line of
    /// game-rule validation on top of the server's. Pure — no AssetDatabase.</summary>
    internal static class ContentMapper
    {
        public static List<Mapped<ItemDefinition>> MapItems(
            IReadOnlyList<GeneratedItemDto> dtos, int rangeMin, int rangeMax)
        {
            var seen = new HashSet<string>();
            var result = new List<Mapped<ItemDefinition>>(dtos.Count);

            foreach (var dto in dtos)
            {
                var errors = new List<string>();
                var name = dto.Name?.Trim() ?? string.Empty;
                if (name.Length == 0)
                {
                    errors.Add("Missing name.");
                }

                var rarity = default(Rarity);
                if (!Enum.TryParse(dto.Rarity, ignoreCase: true, out rarity)
                    || !Enum.IsDefined(typeof(Rarity), rarity))
                {
                    errors.Add($"Invalid rarity '{dto.Rarity}'.");
                }

                if (dto.Power < rangeMin || dto.Power > rangeMax)
                {
                    errors.Add($"Power {dto.Power} is outside [{rangeMin}, {rangeMax}].");
                }

                if (dto.Value < 0)
                {
                    errors.Add("Value is negative.");
                }

                var slug = ContentNaming.Slugify(name);
                if (slug.Length > 0 && !seen.Add(slug))
                {
                    errors.Add($"Duplicate name '{name}' in this batch.");
                }

                var so = ScriptableObject.CreateInstance<ItemDefinition>();
                so.itemName = name;
                so.description = dto.Description ?? string.Empty;
                so.rarity = rarity;
                so.power = dto.Power;
                so.value = dto.Value;

                result.Add(new Mapped<ItemDefinition>
                {
                    Slug = slug,
                    SourceName = name,
                    Value = so,
                    Errors = errors,
                });
            }

            return result;
        }

        public static List<Mapped<EnemyDefinition>> MapEnemies(
            IReadOnlyList<GeneratedEnemyDto> dtos, int rangeMin, int rangeMax)
        {
            var seen = new HashSet<string>();
            var result = new List<Mapped<EnemyDefinition>>(dtos.Count);

            foreach (var dto in dtos)
            {
                var errors = new List<string>();
                var name = dto.Name?.Trim() ?? string.Empty;
                if (name.Length == 0)
                {
                    errors.Add("Missing name.");
                }

                if (dto.Level < rangeMin || dto.Level > rangeMax)
                {
                    errors.Add($"Level {dto.Level} is outside [{rangeMin}, {rangeMax}].");
                }

                if (dto.Health <= 0)
                {
                    errors.Add("Health must be positive.");
                }

                if (dto.Damage < 0)
                {
                    errors.Add("Damage is negative.");
                }

                var slug = ContentNaming.Slugify(name);
                if (slug.Length > 0 && !seen.Add(slug))
                {
                    errors.Add($"Duplicate name '{name}' in this batch.");
                }

                var so = ScriptableObject.CreateInstance<EnemyDefinition>();
                so.enemyName = name;
                so.description = dto.Description ?? string.Empty;
                so.level = dto.Level;
                so.health = dto.Health;
                so.damage = dto.Damage;

                result.Add(new Mapped<EnemyDefinition>
                {
                    Slug = slug,
                    SourceName = name,
                    Value = so,
                    Errors = errors,
                });
            }

            return result;
        }
    }
}
