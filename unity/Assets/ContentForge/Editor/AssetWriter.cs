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
    /// updates existing ones in place (Undo-friendly). Skips Unchanged and Invalid ops.</summary>
    internal static class AssetWriter
    {
        public static int Apply(string targetFolder, IReadOnlyList<ApplyOp> ops)
        {
            EnsureFolder(targetFolder);

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

                    var path = $"{targetFolder}/{op.Slug}.asset";
                    var existing = op.Status == DiffStatus.Changed
                        ? AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)
                        : null;

                    if (existing == null)
                    {
                        op.Value.name = op.Slug;
                        AssetDatabase.CreateAsset(op.Value, path);
                        Undo.RegisterCreatedObjectUndo(op.Value, "Create Content Asset");
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
