using System.Collections.Generic;
using ContentForge;
using ContentForge.Editor;
using NUnit.Framework;

namespace ContentForge.Editor.Tests
{
    public sealed class ContentMapperTests
    {
        private static GeneratedItemDto Item(string name, string rarity, int power, int value = 10) =>
            new() { Name = name, Description = "d", Rarity = rarity, Power = power, Value = value };

        [Test]
        public void MapItems_ValidDto_ProducesPopulatedSo()
        {
            var mapped = ContentMapper.MapItems(new[] { Item("Frost Blade", "rare", 5) }, 1, 10);

            Assert.That(mapped, Has.Count.EqualTo(1));
            Assert.That(mapped[0].IsValid, Is.True);
            Assert.That(mapped[0].Slug, Is.EqualTo("frost-blade"));
            Assert.That(mapped[0].Value.itemName, Is.EqualTo("Frost Blade"));
            Assert.That(mapped[0].Value.rarity, Is.EqualTo(Rarity.Rare));
            Assert.That(mapped[0].Value.power, Is.EqualTo(5));
        }

        [Test]
        public void MapItems_InvalidRarity_MarksInvalid()
        {
            var mapped = ContentMapper.MapItems(new[] { Item("X", "mythic", 5) }, 1, 10);
            Assert.That(mapped[0].IsValid, Is.False);
        }

        [Test]
        public void MapItems_PowerOutOfRange_MarksInvalid()
        {
            var mapped = ContentMapper.MapItems(new[] { Item("X", "rare", 99) }, 1, 10);
            Assert.That(mapped[0].IsValid, Is.False);
        }

        [Test]
        public void MapItems_EmptyName_MarksInvalid()
        {
            var mapped = ContentMapper.MapItems(new[] { Item("  ", "rare", 5) }, 1, 10);
            Assert.That(mapped[0].IsValid, Is.False);
        }

        [Test]
        public void MapItems_NameWithoutLettersOrDigits_MarksInvalid()
        {
            var mapped = ContentMapper.MapItems(new[] { Item("!!!", "rare", 5) }, 1, 10);
            Assert.That(mapped[0].IsValid, Is.False);
        }

        [Test]
        public void MapItems_DuplicateNamesInBatch_MarksSecondInvalid()
        {
            var mapped = ContentMapper.MapItems(
                new[] { Item("Frost Blade", "rare", 5), Item("frost  blade", "rare", 6) }, 1, 10);

            Assert.That(mapped[0].IsValid, Is.True);
            Assert.That(mapped[1].IsValid, Is.False);
        }

        [Test]
        public void MapEnemies_ValidDto_ProducesPopulatedSo()
        {
            var dto = new GeneratedEnemyDto { Name = "Ice Wraith", Description = "d", Level = 3, Health = 40, Damage = 8 };
            var mapped = ContentMapper.MapEnemies(new[] { dto }, 1, 10);

            Assert.That(mapped[0].IsValid, Is.True);
            Assert.That(mapped[0].Value.health, Is.EqualTo(40));
        }

        [Test]
        public void MapEnemies_NonPositiveHealth_MarksInvalid()
        {
            var dto = new GeneratedEnemyDto { Name = "X", Description = "d", Level = 3, Health = 0, Damage = 8 };
            var mapped = ContentMapper.MapEnemies(new[] { dto }, 1, 10);
            Assert.That(mapped[0].IsValid, Is.False);
        }

        [Test]
        public void MapEnemies_NameWithoutLettersOrDigits_MarksInvalid()
        {
            var dto = new GeneratedEnemyDto { Name = "???", Description = "d", Level = 3, Health = 40, Damage = 8 };
            var mapped = ContentMapper.MapEnemies(new[] { dto }, 1, 10);
            Assert.That(mapped[0].IsValid, Is.False);
        }
    }
}
