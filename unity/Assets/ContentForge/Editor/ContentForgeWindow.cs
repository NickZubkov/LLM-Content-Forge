using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ContentForge.Editor
{
    /// <summary>
    /// Editor window that requests generated game content from the Content Forge API and shows the
    /// raw response. Mapping the response into ScriptableObjects is a later step.
    /// </summary>
    public sealed class ContentForgeWindow : EditorWindow
    {
        private enum ContentKind { Item, Enemy }

        private string _serverUrl = "http://localhost:8080";
        private ContentKind _contentType = ContentKind.Item;
        private int _count = 5;
        private string _theme = "frozen dungeon";
        private int _levelMin = 1;
        private int _levelMax = 10;

        private bool _isBusy;
        private string _status = string.Empty;
        private string _output = string.Empty;
        private Vector2 _outputScroll;
        private CancellationTokenSource _cts;

        [MenuItem("Window/Content Forge")]
        public static void Open() => GetWindow<ContentForgeWindow>("Content Forge");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Content Forge", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_isBusy))
            {
                _serverUrl = EditorGUILayout.TextField("Server URL", _serverUrl);
                _contentType = (ContentKind)EditorGUILayout.EnumPopup("Content Type", _contentType);
                _count = EditorGUILayout.IntSlider("Count", _count, 1, 50);
                _theme = EditorGUILayout.TextField("Theme", _theme);

                EditorGUILayout.BeginHorizontal();
                _levelMin = EditorGUILayout.IntField("Level Min", _levelMin);
                _levelMax = EditorGUILayout.IntField("Level Max", _levelMax);
                EditorGUILayout.EndHorizontal();
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
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.HelpBox(_status, _isBusy ? MessageType.Info : MessageType.None);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
            _outputScroll = EditorGUILayout.BeginScrollView(_outputScroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_output, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private async UniTaskVoid GenerateAsync()
        {
            _isBusy = true;
            _status = "Generating…";
            _output = string.Empty;
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
                _output = Prettify(raw);
                _status = "Done.";
            }
            catch (OperationCanceledException)
            {
                _status = "Cancelled.";
            }
            catch (ContentForgeException ex)
            {
                _status = "Error.";
                _output = ex.Message;
            }
            catch (Exception ex)
            {
                _status = "Unexpected error.";
                _output = ex.ToString();
            }
            finally
            {
                _isBusy = false;
                _cts?.Dispose();
                _cts = null;
                Repaint();
            }
        }

        private static string Prettify(string json)
        {
            try
            {
                return JToken.Parse(json).ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch
            {
                return json; // not JSON — show as received
            }
        }
    }
}
