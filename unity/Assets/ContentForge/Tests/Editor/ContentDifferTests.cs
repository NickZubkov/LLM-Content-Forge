using System.Collections.Generic;
using ContentForge;
using ContentForge.Editor;
using NUnit.Framework;
using UnityEngine;

namespace ContentForge.Editor.Tests
{
    public sealed class ContentDifferTests
    {
        private static Mapped<ItemDefinition> ValidItem(string slug, string name, Rarity rarity, int power)
        {
            var so = ScriptableObject.CreateInstance<ItemDefinition>();
            so.itemName = name;
            so.rarity = rarity;
            so.power = power;
            return new Mapped<ItemDefinition>
            {
                Slug = slug, SourceName = name, Value = so, Errors = new List<string>(),
            };
        }

        private static ItemDefinition ExistingItem(string name, Rarity rarity, int power)
        {
            var so = ScriptableObject.CreateInstance<ItemDefinition>();
            so.itemName = name;
            so.rarity = rarity;
            so.power = power;
            return so;
        }

        [Test]
        public void DiffItems_NoExisting_IsNew()
        {
            var gen = new[] { ValidItem("frost-blade", "Frost Blade", Rarity.Rare, 5) };
            var result = ContentDiffer.DiffItems(gen, new Dictionary<string, ItemDefinition>());

            Assert.That(result[0].Status, Is.EqualTo(DiffStatus.New));
        }

        [Test]
        public void DiffItems_IdenticalExisting_IsUnchanged()
        {
            var gen = new[] { ValidItem("frost-blade", "Frost Blade", Rarity.Rare, 5) };
            var existing = new Dictionary<string, ItemDefinition>
            {
                ["frost-blade"] = ExistingItem("Frost Blade", Rarity.Rare, 5),
            };

            var result = ContentDiffer.DiffItems(gen, existing);

            Assert.That(result[0].Status, Is.EqualTo(DiffStatus.Unchanged));
            Assert.That(result[0].Changes, Is.Empty);
        }

        [Test]
        public void DiffItems_ChangedField_IsChangedWithDetail()
        {
            var gen = new[] { ValidItem("frost-blade", "Frost Blade", Rarity.Rare, 8) };
            var existing = new Dictionary<string, ItemDefinition>
            {
                ["frost-blade"] = ExistingItem("Frost Blade", Rarity.Rare, 5),
            };

            var result = ContentDiffer.DiffItems(gen, existing);

            Assert.That(result[0].Status, Is.EqualTo(DiffStatus.Changed));
            Assert.That(result[0].Changes, Has.Count.EqualTo(1));
            Assert.That(result[0].Changes[0].Field, Is.EqualTo("power"));
            Assert.That(result[0].Changes[0].OldValue, Is.EqualTo("5"));
            Assert.That(result[0].Changes[0].NewValue, Is.EqualTo("8"));
        }

        [Test]
        public void DiffItems_InvalidMapped_IsInvalid()
        {
            var so = ScriptableObject.CreateInstance<ItemDefinition>();
            var gen = new[]
            {
                new Mapped<ItemDefinition>
                {
                    Slug = "x", SourceName = "X", Value = so,
                    Errors = new List<string> { "bad" },
                },
            };

            var result = ContentDiffer.DiffItems(gen, new Dictionary<string, ItemDefinition>());

            Assert.That(result[0].Status, Is.EqualTo(DiffStatus.Invalid));
        }
    }
}
