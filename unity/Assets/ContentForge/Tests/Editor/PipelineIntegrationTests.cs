using System.Collections.Generic;
using System.IO;
using System.Linq;
using ContentForge;
using ContentForge.Editor;
using NUnit.Framework;
using UnityEditor;

namespace ContentForge.Editor.Tests
{
    /// <summary>Exercises the full editor pipeline the window drives — parse, map, load existing,
    /// diff, apply — end to end on real assets, without the GUI or a live server.</summary>
    public sealed class PipelineIntegrationTests
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

        // Mirrors ContentForgeWindow.LoadExisting: key existing assets by their file-name slug.
        private static Dictionary<string, ItemDefinition> LoadExistingItems(string folder)
        {
            var map = new Dictionary<string, ItemDefinition>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return map;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:ItemDefinition", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (so != null)
                {
                    map[Path.GetFileNameWithoutExtension(path)] = so;
                }
            }

            return map;
        }

        private static List<ApplyOp> ToOps(IEnumerable<DiffEntry<ItemDefinition>> diff) =>
            diff.Select(d => new ApplyOp(d.Status, d.Generated.Slug, d.Generated.Value)).ToList();

        [Test]
        public void FullPipeline_CreatesThenUpdatesInPlace()
        {
            const string first =
                "{\"contentType\":\"Item\",\"content\":[" +
                "{\"name\":\"Frost Blade\",\"description\":\"cold\",\"rarity\":\"rare\",\"power\":5,\"value\":100}]}";

            var parsed = GenerateResponseParser.Parse(first);
            var mapped = ContentMapper.MapItems(parsed.Items, 1, 10);
            var diff = ContentDiffer.DiffItems(mapped, LoadExistingItems(TempFolder));

            Assert.That(diff[0].Status, Is.EqualTo(DiffStatus.New));
            AssetWriter.Apply(TempFolder, ToOps(diff));

            var created = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{TempFolder}/frost-blade.asset");
            Assert.That(created, Is.Not.Null);
            Assert.That(created.power, Is.EqualTo(5));

            // Second run: same name, changed power -> Changed -> updated in place, no duplicate.
            const string second =
                "{\"contentType\":\"Item\",\"content\":[" +
                "{\"name\":\"Frost Blade\",\"description\":\"cold\",\"rarity\":\"rare\",\"power\":8,\"value\":100}]}";

            var parsed2 = GenerateResponseParser.Parse(second);
            var mapped2 = ContentMapper.MapItems(parsed2.Items, 1, 10);
            var diff2 = ContentDiffer.DiffItems(mapped2, LoadExistingItems(TempFolder));

            Assert.That(diff2[0].Status, Is.EqualTo(DiffStatus.Changed));
            Assert.That(diff2[0].Changes.Any(c => c.Field == "power"), Is.True);
            AssetWriter.Apply(TempFolder, ToOps(diff2));

            var updated = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{TempFolder}/frost-blade.asset");
            Assert.That(updated.power, Is.EqualTo(8));

            var all = AssetDatabase.FindAssets("t:ItemDefinition", new[] { TempFolder });
            Assert.That(all.Length, Is.EqualTo(1));
        }
    }
}
