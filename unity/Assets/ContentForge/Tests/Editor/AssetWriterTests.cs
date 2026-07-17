using System.Collections.Generic;
using ContentForge;
using ContentForge.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ContentForge.Editor.Tests
{
    public sealed class AssetWriterTests
    {
        private const string TempFolder = "Assets/ContentForgeTemp";

        [TearDown]
        public void Cleanup()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }
        }

        [Test]
        public void Apply_New_CreatesAssetWithFields()
        {
            var so = ScriptableObject.CreateInstance<ItemDefinition>();
            so.itemName = "Frost Blade";
            so.rarity = Rarity.Rare;
            so.power = 7;
            so.value = 100;

            var written = AssetWriter.Apply(
                TempFolder, new List<ApplyOp> { new(DiffStatus.New, "frost-blade", so) });

            Assert.That(written, Is.EqualTo(1));
            var loaded = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{TempFolder}/frost-blade.asset");
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.power, Is.EqualTo(7));
            Assert.That(loaded.rarity, Is.EqualTo(Rarity.Rare));
        }

        [Test]
        public void Apply_Changed_UpdatesExistingAsset()
        {
            var original = ScriptableObject.CreateInstance<ItemDefinition>();
            original.itemName = "Frost Blade";
            original.power = 5;
            AssetWriter.Apply(TempFolder, new List<ApplyOp> { new(DiffStatus.New, "frost-blade", original) });

            var updated = ScriptableObject.CreateInstance<ItemDefinition>();
            updated.itemName = "Frost Blade";
            updated.power = 9;
            AssetWriter.Apply(TempFolder, new List<ApplyOp> { new(DiffStatus.Changed, "frost-blade", updated) });

            var loaded = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{TempFolder}/frost-blade.asset");
            Assert.That(loaded.power, Is.EqualTo(9));
        }

        [Test]
        public void Apply_Unchanged_WritesNothing()
        {
            var so = ScriptableObject.CreateInstance<ItemDefinition>();
            so.itemName = "X";
            var written = AssetWriter.Apply(
                TempFolder, new List<ApplyOp> { new(DiffStatus.Unchanged, "x", so) });

            Assert.That(written, Is.EqualTo(0));
        }

        [Test]
        public void Apply_FolderOutsideAssets_Throws()
        {
            var so = ScriptableObject.CreateInstance<ItemDefinition>();
            Assert.Throws<System.ArgumentException>(
                () => AssetWriter.Apply("Packages/Foo", new List<ApplyOp> { new(DiffStatus.New, "x", so) }));
        }

        [Test]
        public void Apply_ToleratesBackslashesAndTrailingSlash()
        {
            var so = ScriptableObject.CreateInstance<ItemDefinition>();
            so.itemName = "Frost Blade";

            var written = AssetWriter.Apply(
                "Assets\\ContentForgeTemp\\", new List<ApplyOp> { new(DiffStatus.New, "frost-blade", so) });

            Assert.That(written, Is.EqualTo(1));
            Assert.That(
                AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{TempFolder}/frost-blade.asset"),
                Is.Not.Null);
        }
    }
}
