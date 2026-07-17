using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ContentForge.Editor
{
    /// <summary>One create-or-update instruction for <see cref="AssetWriter"/>.</summary>
    internal readonly struct ApplyOp
    {
        public readonly DiffStatus Status;
        public readonly string Slug;
        public readonly ScriptableObject Value;

        public ApplyOp(DiffStatus status, string slug, ScriptableObject value)
        {
            Status = status;
            Slug = slug;
            Value = value;
        }
    }

    /// <summary>The only component that touches AssetDatabase. Creates new .asset files and
    /// updates existing ones in place (updates are undoable; asset creation is not).
    /// Skips Unchanged and Invalid ops.</summary>
    internal static class AssetWriter
    {
        public static int Apply(string targetFolder, IReadOnlyList<ApplyOp> ops)
        {
            var folder = NormalizeFolder(targetFolder);
            EnsureFolder(folder);

            var written = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var op in ops)
                {
                    if (op.Status is DiffStatus.Unchanged or DiffStatus.Invalid)
                    {
                        continue;
                    }

                    var path = $"{folder}/{op.Slug}.asset";
                    var existing = op.Status == DiffStatus.Changed
                        ? AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)
                        : null;

                    if (existing == null)
                    {
                        op.Value.name = op.Slug;
                        AssetDatabase.CreateAsset(op.Value, path);
                    }
                    else
                    {
                        // CopySerialized also copies m_Name; preserve the existing asset's name.
                        var savedName = existing.name;
                        Undo.RecordObject(existing, "Update Content Asset");
                        EditorUtility.CopySerialized(op.Value, existing);
                        existing.name = savedName;
                        EditorUtility.SetDirty(existing);
                    }

                    written++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return written;
        }

        /// <summary>Accepts forward/backward slashes and a trailing separator; rejects anything
        /// outside Assets/ before any folder gets created.</summary>
        private static string NormalizeFolder(string folder)
        {
            var normalized = (folder ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (normalized != "Assets" && !normalized.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Target folder must be inside 'Assets/', got '{folder}'.", nameof(folder));
            }

            return normalized;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parts = folder.Split('/');
            var current = parts[0]; // "Assets"
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
