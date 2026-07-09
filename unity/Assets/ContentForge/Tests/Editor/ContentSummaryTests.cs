using ContentForge;
using ContentForge.Editor;
using NUnit.Framework;
using UnityEngine;

namespace ContentForge.Editor.Tests
{
    public sealed class ContentSummaryTests
    {
        [Test]
        public void Describe_Item_ListsRarityPowerValue()
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.rarity = Rarity.Rare;
            item.power = 7;
            item.value = 100;

            Assert.That(ContentSummary.Describe(item), Is.EqualTo("Rare · power 7 · value 100"));
        }

        [Test]
        public void Describe_Enemy_ListsLevelHealthDamage()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            enemy.level = 3;
            enemy.health = 40;
            enemy.damage = 8;

            Assert.That(ContentSummary.Describe(enemy), Is.EqualTo("level 3 · hp 40 · dmg 8"));
        }
    }
}
