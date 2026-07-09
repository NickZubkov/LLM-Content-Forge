using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ContentForge.Editor
{
    /// <summary>Outcome of comparing a generated entry against the target folder.</summary>
    internal enum DiffStatus
    {
        New,
        Changed,
        Unchanged,
        Invalid
    }

    /// <summary>A single field that differs between an existing asset and its generated replacement.</summary>
    internal sealed class FieldChange
    {
        public readonly string Field;
        public readonly string OldValue;
        public readonly string NewValue;

        public FieldChange(string field, string oldValue, string newValue)
        {
            Field = field;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>A generated entry paired with how it compares to what is already on disk.</summary>
    internal sealed class DiffEntry<T> where T : ScriptableObject
    {
        public Mapped<T> Generated;
        public DiffStatus Status;
        public IReadOnlyList<FieldChange> Changes = Array.Empty<FieldChange>();
        public T Existing;
    }

    /// <summary>Compares mapped content against already-loaded existing assets (keyed by slug).
    /// Pure — the caller loads existing assets via AssetDatabase and passes them in.</summary>
    internal static class ContentDiffer
    {
        public static List<DiffEntry<ItemDefinition>> DiffItems(
            IReadOnlyList<Mapped<ItemDefinition>> generated,
            IReadOnlyDictionary<string, ItemDefinition> existingBySlug)
        {
            var result = new List<DiffEntry<ItemDefinition>>(generated.Count);
            foreach (var m in generated)
            {
                if (!m.IsValid)
                {
                    result.Add(new DiffEntry<ItemDefinition> { Generated = m, Status = DiffStatus.Invalid });
                    continue;
                }

                if (!existingBySlug.TryGetValue(m.Slug, out var existing) || existing == null)
                {
                    result.Add(new DiffEntry<ItemDefinition> { Generated = m, Status = DiffStatus.New });
                    continue;
                }

                var changes = CompareItems(existing, m.Value);
                result.Add(new DiffEntry<ItemDefinition>
                {
                    Generated = m,
                    Existing = existing,
                    Status = changes.Count == 0 ? DiffStatus.Unchanged : DiffStatus.Changed,
                    Changes = changes,
                });
            }

            return result;
        }

        public static List<DiffEntry<EnemyDefinition>> DiffEnemies(
            IReadOnlyList<Mapped<EnemyDefinition>> generated,
            IReadOnlyDictionary<string, EnemyDefinition> existingBySlug)
        {
            var result = new List<DiffEntry<EnemyDefinition>>(generated.Count);
            foreach (var m in generated)
            {
                if (!m.IsValid)
                {
                    result.Add(new DiffEntry<EnemyDefinition> { Generated = m, Status = DiffStatus.Invalid });
                    continue;
                }

                if (!existingBySlug.TryGetValue(m.Slug, out var existing) || existing == null)
                {
                    result.Add(new DiffEntry<EnemyDefinition> { Generated = m, Status = DiffStatus.New });
                    continue;
                }

                var changes = CompareEnemies(existing, m.Value);
                result.Add(new DiffEntry<EnemyDefinition>
                {
                    Generated = m,
                    Existing = existing,
                    Status = changes.Count == 0 ? DiffStatus.Unchanged : DiffStatus.Changed,
                    Changes = changes,
                });
            }

            return result;
        }

        private static List<FieldChange> CompareItems(ItemDefinition a, ItemDefinition b)
        {
            var c = new List<FieldChange>();
            AddText(c, "name", a.itemName, b.itemName);
            AddText(c, "description", a.description, b.description);
            AddText(c, "rarity", a.rarity.ToString(), b.rarity.ToString());
            AddInt(c, "power", a.power, b.power);
            AddInt(c, "value", a.value, b.value);
            return c;
        }

        private static List<FieldChange> CompareEnemies(EnemyDefinition a, EnemyDefinition b)
        {
            var c = new List<FieldChange>();
            AddText(c, "name", a.enemyName, b.enemyName);
            AddText(c, "description", a.description, b.description);
            AddInt(c, "level", a.level, b.level);
            AddInt(c, "health", a.health, b.health);
            AddInt(c, "damage", a.damage, b.damage);
            return c;
        }

        private static void AddText(List<FieldChange> c, string field, string oldV, string newV)
        {
            oldV ??= string.Empty;
            newV ??= string.Empty;
            if (!string.Equals(oldV, newV, StringComparison.Ordinal))
            {
                c.Add(new FieldChange(field, oldV, newV));
            }
        }

        private static void AddInt(List<FieldChange> c, string field, int oldV, int newV)
        {
            if (oldV != newV)
            {
                c.Add(new FieldChange(
                    field,
                    oldV.ToString(CultureInfo.InvariantCulture),
                    newV.ToString(CultureInfo.InvariantCulture)));
            }
        }
    }
}
