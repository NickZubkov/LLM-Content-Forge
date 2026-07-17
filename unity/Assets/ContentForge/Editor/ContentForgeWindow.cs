using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ContentForge.Editor
{
    /// <summary>
    /// Editor window that requests generated game content from the Content Forge API, shows a
    /// New/Changed/Unchanged diff preview, and writes selected entries to ScriptableObject assets.
    /// </summary>
    public sealed class ContentForgeWindow : EditorWindow
    {
        private enum ContentKind { Item, Enemy }

        // One preview row, unified across item/enemy so OnGUI has a single render path.
        private sealed class Row
        {
            public string Title;
            public DiffStatus Status;
            public string Params;
            public string Description;
            public string Detail;
            public bool Selected;
            public ApplyOp Op;
        }

        private string _serverUrl = "http://localhost:8080";
        private ContentKind _contentType = ContentKind.Item;
        private int _count = 5;
        private string _theme = "frozen dungeon";
        private int _levelMin = 1;
        private int _levelMax = 10;
        private string _targetFolder = "Assets/ContentForge/Generated/Items";

        private bool _isBusy;
        private string _status = string.Empty;
        private string _error = string.Empty;
        private List<Row> _rows;
        private Vector2 _scroll;
        private CancellationTokenSource _cts;

        [MenuItem("Window/Content Forge")]
        public static void Open() => GetWindow<ContentForgeWindow>("Content Forge");

        private void OnDisable()
        {
            // Window closing or domain reload — cancel any in-flight request so its
            // continuation does not touch a destroyed window.
            _cts?.Cancel();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Content Forge", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_isBusy))
            {
                _serverUrl = EditorGUILayout.TextField("Server URL", _serverUrl);

                EditorGUI.BeginChangeCheck();
                _contentType = (ContentKind)EditorGUILayout.EnumPopup("Content Type", _contentType);
                if (EditorGUI.EndChangeCheck())
                {
                    _targetFolder = _contentType == ContentKind.Item
                        ? "Assets/ContentForge/Generated/Items"
                        : "Assets/ContentForge/Generated/Enemies";
                }

                _count = EditorGUILayout.IntSlider("Count", _count, 1, 50);
                _theme = EditorGUILayout.TextField("Theme", _theme);

                EditorGUILayout.BeginHorizontal();
                _levelMin = EditorGUILayout.IntField("Level Min", _levelMin);
                _levelMax = EditorGUILayout.IntField("Level Max", _levelMax);
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                _targetFolder = EditorGUILayout.TextField("Target Folder", _targetFolder);
                if (EditorGUI.EndChangeCheck())
                {
                    // The preview was diffed against the previous folder — it is stale now.
                    _rows = null;
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(_isBusy))
            {
                if (GUILayout.Button("Generate"))
                {
                    GenerateAsync().Forget();
                }
            }

            using (new EditorGUI.DisabledScope(!_isBusy))
            {
                if (GUILayout.Button("Cancel"))
                {
                    _cts?.Cancel();
                }
            }

            // Discards the unapplied preview. Nothing is written until Apply, so this only
            // drops the in-memory generated entries — no assets are touched.
            using (new EditorGUI.DisabledScope(_isBusy || _rows == null))
            {
                if (GUILayout.Button("Clear"))
                {
                    _rows = null;
                    _status = string.Empty;
                    _error = string.Empty;
                    Repaint();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.HelpBox(_status, _isBusy ? MessageType.Info : MessageType.None);
            }

            if (!string.IsNullOrEmpty(_error))
            {
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            }

            DrawPreview();
        }

        private void DrawPreview()
        {
            if (_rows == null || _rows.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            foreach (var row in _rows)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                var applicable = row.Status is DiffStatus.New or DiffStatus.Changed;
                using (new EditorGUI.DisabledScope(!applicable))
                {
                    row.Selected = EditorGUILayout.Toggle(row.Selected, GUILayout.Width(18));
                }

                var prev = GUI.color;
                GUI.color = StatusColor(row.Status);
                EditorGUILayout.LabelField($"[{row.Status}]", GUILayout.Width(90));
                GUI.color = prev;

                EditorGUILayout.LabelField(row.Title, EditorStyles.boldLabel, GUILayout.Width(160));
                EditorGUILayout.LabelField(row.Params, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                // Generated description (free text) on its own wrapped line.
                if (!string.IsNullOrEmpty(row.Description))
                {
                    EditorGUILayout.LabelField("    " + row.Description, EditorStyles.wordWrappedMiniLabel);
                }

                // Last line: field diff (Changed) or validation errors (Invalid).
                if (!string.IsNullOrEmpty(row.Detail))
                {
                    EditorGUILayout.LabelField("    " + row.Detail, EditorStyles.wordWrappedMiniLabel);
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            var applyCount = _rows.Count(r => r.Selected);
            using (new EditorGUI.DisabledScope(_isBusy || applyCount == 0))
            {
                if (GUILayout.Button($"Apply Selected ({applyCount})"))
                {
                    ApplySelected();
                }
            }
        }

        private static Color StatusColor(DiffStatus status) => status switch
        {
            DiffStatus.New => Color.green,
            DiffStatus.Changed => Color.yellow,
            DiffStatus.Invalid => Color.red,
            _ => Color.gray,
        };

        private async UniTaskVoid GenerateAsync()
        {
            _isBusy = true;
            _status = "Generating…";
            _error = string.Empty;
            _rows = null;
            _cts = new CancellationTokenSource();
            Repaint();

            try
            {
                var request = new GenerateRequestDto
                {
                    ContentType = _contentType.ToString(),
                    Count = _count,
                    Theme = _theme,
                    LevelRange = new LevelRangeDto { Min = _levelMin, Max = _levelMax },
                };

                var raw = await ContentForgeClient.GenerateAsync(_serverUrl, request, _cts.Token);
                _rows = BuildRows(GenerateResponseParser.Parse(raw));
                _status = $"{_rows.Count} entries. Review and apply.";
            }
            catch (OperationCanceledException)
            {
                _status = "Cancelled.";
            }
            catch (ContentForgeException ex)
            {
                _status = string.Empty;
                _error = ex.Message;
            }
            catch (GenerateResponseParseException ex)
            {
                _status = string.Empty;
                _error = $"Could not read the response: {ex.Message}";
            }
            catch (Exception ex)
            {
                _status = string.Empty;
                _error = ex.ToString();
            }
            finally
            {
                _isBusy = false;
                _cts?.Dispose();
                _cts = null;
                if (this != null) // destroyed while the request was in flight
                {
                    Repaint();
                }
            }
        }

        private List<Row> BuildRows(ParsedGeneration parsed)
        {
            return parsed.ContentType == GeneratedContentType.Item
                ? BuildItemRows(parsed.Items)
                : BuildEnemyRows(parsed.Enemies);
        }

        private List<Row> BuildItemRows(IReadOnlyList<GeneratedItemDto> dtos)
        {
            var mapped = ContentMapper.MapItems(dtos, _levelMin, _levelMax);
            var existing = LoadExisting<ItemDefinition>(_targetFolder);
            var diff = ContentDiffer.DiffItems(mapped, existing);
            return diff.Select(d =>
                ToRow(d, ContentSummary.Describe(d.Generated.Value), d.Generated.Value.description)).ToList();
        }

        private List<Row> BuildEnemyRows(IReadOnlyList<GeneratedEnemyDto> dtos)
        {
            var mapped = ContentMapper.MapEnemies(dtos, _levelMin, _levelMax);
            var existing = LoadExisting<EnemyDefinition>(_targetFolder);
            var diff = ContentDiffer.DiffEnemies(mapped, existing);
            return diff.Select(d =>
                ToRow(d, ContentSummary.Describe(d.Generated.Value), d.Generated.Value.description)).ToList();
        }

        private static Row ToRow<T>(DiffEntry<T> entry, string paramsSummary, string description)
            where T : ScriptableObject
        {
            // The params summary is shown for every row; Detail is the status-specific extra
            // (field diff for Changed, validation errors for Invalid).
            var detail = entry.Status switch
            {
                DiffStatus.Invalid => string.Join("; ", entry.Generated.Errors),
                DiffStatus.Changed => string.Join(", ",
                    entry.Changes.Select(c => $"{c.Field}: {c.OldValue} → {c.NewValue}")),
                _ => string.Empty,
            };

            return new Row
            {
                Title = string.IsNullOrEmpty(entry.Generated.SourceName)
                    ? "(no name)"
                    : entry.Generated.SourceName,
                Status = entry.Status,
                Params = paramsSummary,
                Description = description,
                Detail = detail,
                Selected = entry.Status is DiffStatus.New or DiffStatus.Changed,
                Op = new ApplyOp(entry.Status, entry.Generated.Slug, entry.Generated.Value),
            };
        }

        private static Dictionary<string, T> LoadExisting<T>(string folder) where T : ScriptableObject
        {
            var map = new Dictionary<string, T>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return map;
            }

            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<T>(path);
                if (so != null)
                {
                    map[Path.GetFileNameWithoutExtension(path)] = so;
                }
            }

            return map;
        }

        private void ApplySelected()
        {
            var ops = _rows.Where(r => r.Selected).Select(r => r.Op).ToList();
            try
            {
                var written = AssetWriter.Apply(_targetFolder, ops);
                _status = $"Applied {written} asset(s) to {_targetFolder}.";
                _error = string.Empty;
                _rows = null; // force a fresh Generate before the next apply
            }
            catch (Exception ex)
            {
                _status = string.Empty;
                _error = ex.Message;
            }
            Repaint();
        }
    }
}
