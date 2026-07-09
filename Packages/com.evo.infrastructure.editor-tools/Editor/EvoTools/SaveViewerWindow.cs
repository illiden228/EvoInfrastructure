using System;
using System.Collections.Generic;
using System.IO;
using Evo.Infrastructure.Services.Save;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools
{
    public sealed class SaveViewerWindow : EditorWindow
    {
        private const string MenuPath = "EvoTools/Save Viewer";
        private const string EditorPrefsPrefix = "EvoTools.SaveViewer.";
        private const float MinJsonHeight = 72f;
        private const float MaxJsonHeight = 520f;

        [SerializeField] private string fileName = SaveStorageDefaults.FileName;
        [SerializeField] private string playerPrefsKey = SaveStorageDefaults.PlayerPrefsKey;
        [SerializeField] private SaveEnvelope envelope;
        [SerializeField] private string rawEnvelopeJson = string.Empty;
        [SerializeField] private Vector2 scrollPosition;
        [SerializeField] private Vector2 fileListScrollPosition;
        [SerializeField] private Vector2 rawJsonScrollPosition;
        [SerializeField] private float payloadJsonHeight = 120f;
        [SerializeField] private float rawJsonHeight = 220f;

        private readonly List<string> files = new();
        private readonly List<Vector2> payloadJsonScrollPositions = new();
        private GUIStyle jsonTextAreaStyle;
        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.Info;

        [MenuItem(MenuPath, false, 81)]
        public static void Open()
        {
            var window = GetWindow<SaveViewerWindow>("Save Viewer");
            window.minSize = new Vector2(720f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            fileName = EditorPrefs.GetString(BuildPrefsKey(nameof(fileName)), SaveStorageDefaults.FileName);
            playerPrefsKey = EditorPrefs.GetString(BuildPrefsKey(nameof(playerPrefsKey)), SaveStorageDefaults.PlayerPrefsKey);
            RefreshFiles();
            EnsureEnvelope();
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(BuildPrefsKey(nameof(fileName)), NormalizeFileName(fileName));
            EditorPrefs.SetString(BuildPrefsKey(nameof(playerPrefsKey)), NormalizePlayerPrefsKey(playerPrefsKey));
        }

        private void OnGUI()
        {
            EnsureStyles();

            DrawSource();
            DrawStatus();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawEnvelope();
            DrawPayloads();
            DrawRawJson();
            EditorGUILayout.EndScrollView();
        }

        private void DrawSource()
        {
            EditorGUILayout.LabelField("Save Source", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            fileName = EditorGUILayout.TextField("File Name", fileName);
            playerPrefsKey = EditorGUILayout.TextField("PlayerPrefs Key", playerPrefsKey);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load File"))
            {
                LoadFromFile();
            }

            if (GUILayout.Button("Save File"))
            {
                SaveToFile();
            }

            if (GUILayout.Button("Delete File"))
            {
                DeleteFile();
            }

            if (GUILayout.Button("Open Folder"))
            {
                EditorUtility.RevealInFinder(UnityEngine.Application.persistentDataPath);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load PlayerPrefs"))
            {
                LoadFromPlayerPrefs();
            }

            if (GUILayout.Button("Save PlayerPrefs"))
            {
                SaveToPlayerPrefs();
            }

            if (GUILayout.Button("Delete PlayerPrefs"))
            {
                DeletePlayerPrefs();
            }

            if (GUILayout.Button("Refresh Files"))
            {
                RefreshFiles();
            }

            EditorGUILayout.EndHorizontal();

            DrawFileList();
            EditorGUILayout.EndVertical();
        }

        private void DrawFileList()
        {
            if (files.Count == 0)
            {
                EditorGUILayout.HelpBox("No .json saves found in persistentDataPath.", MessageType.Info);
                return;
            }

            fileListScrollPosition = EditorGUILayout.BeginScrollView(fileListScrollPosition, GUILayout.MaxHeight(96f));
            for (var i = 0; i < files.Count; i++)
            {
                var saveFileName = files[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(saveFileName, EditorStyles.miniLabel);
                if (GUILayout.Button("Use", GUILayout.Width(52f)))
                {
                    fileName = saveFileName;
                }

                if (GUILayout.Button("Load", GUILayout.Width(52f)))
                {
                    fileName = saveFileName;
                    LoadFromFile();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatus()
        {
            if (string.IsNullOrWhiteSpace(statusMessage))
            {
                return;
            }

            EditorGUILayout.HelpBox(statusMessage, statusType);
        }

        private void DrawEnvelope()
        {
            EnsureEnvelope();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Envelope", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            envelope.schemaVersion = Mathf.Max(1, EditorGUILayout.IntField("Schema Version", envelope.schemaVersion));
            envelope.updatedAtUnixMs = EditorGUILayout.LongField("Updated At Unix Ms", envelope.updatedAtUnixMs);
            EditorGUILayout.LabelField("Updated At UTC", FormatUnixMs(envelope.updatedAtUnixMs, true));
            EditorGUILayout.LabelField("Updated At Local", FormatUnixMs(envelope.updatedAtUnixMs, false));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Empty Envelope"))
            {
                envelope = new SaveEnvelope();
                SyncRawJsonFromEnvelope();
                SetStatus("Created empty envelope.", MessageType.Info);
            }

            if (GUILayout.Button("Update Timestamp"))
            {
                TouchEnvelope();
                SyncRawJsonFromEnvelope();
            }

            EditorGUILayout.EndHorizontal();
            DrawValidation();
            EditorGUILayout.EndVertical();
        }

        private void DrawPayloads()
        {
            EnsureEnvelope();
            if (envelope.payloads == null)
            {
                envelope.payloads = new List<SavePayloadData>();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Payloads", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            payloadJsonHeight = EditorGUILayout.Slider(payloadJsonHeight, MinJsonHeight, MaxJsonHeight, GUILayout.Width(190f));
            if (GUILayout.Button("Add Payload", GUILayout.Width(120f)))
            {
                envelope.payloads.Add(new SavePayloadData { key = "default", version = 1, json = "{}" });
                SyncRawJsonFromEnvelope();
            }

            EditorGUILayout.EndHorizontal();
            EnsurePayloadScrollCount(envelope.payloads.Count);

            for (var i = 0; i < envelope.payloads.Count; i++)
            {
                var payload = envelope.payloads[i];
                if (payload == null)
                {
                    payload = new SavePayloadData();
                    envelope.payloads[i] = payload;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                payload.key = EditorGUILayout.TextField("Key", payload.key);
                if (GUILayout.Button("Remove", GUILayout.Width(82f)))
                {
                    envelope.payloads.RemoveAt(i);
                    SyncRawJsonFromEnvelope();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                payload.version = Mathf.Max(1, EditorGUILayout.IntField("Version", payload.version));
                EditorGUILayout.LabelField("Json");
                var payloadScroll = payloadJsonScrollPositions[i];
                payload.json = DrawScrollableTextArea(payload.json ?? string.Empty, ref payloadScroll, payloadJsonHeight);
                payloadJsonScrollPositions[i] = payloadScroll;
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawRawJson()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Raw Envelope Json", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            rawJsonHeight = EditorGUILayout.Slider(rawJsonHeight, MinJsonHeight, MaxJsonHeight, GUILayout.Width(190f));
            EditorGUILayout.EndHorizontal();

            rawEnvelopeJson = DrawScrollableTextArea(rawEnvelopeJson ?? string.Empty, ref rawJsonScrollPosition, rawJsonHeight);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy From Envelope"))
            {
                SyncRawJsonFromEnvelope();
            }

            if (GUILayout.Button("Apply To Envelope"))
            {
                ApplyRawJsonToEnvelope();
            }

            if (GUILayout.Button("Copy To Clipboard"))
            {
                SyncRawJsonFromEnvelope();
                EditorGUIUtility.systemCopyBuffer = rawEnvelopeJson;
                SetStatus("Envelope json copied to clipboard.", MessageType.Info);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidation()
        {
            if (envelope == null)
            {
                EditorGUILayout.HelpBox("Envelope is null.", MessageType.Error);
                return;
            }

            if (envelope.schemaVersion <= 0)
            {
                EditorGUILayout.HelpBox("Schema version must be greater than zero.", MessageType.Error);
            }

            if (envelope.payloads == null || envelope.payloads.Count == 0)
            {
                EditorGUILayout.HelpBox("Envelope has no payloads.", MessageType.Info);
                return;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < envelope.payloads.Count; i++)
            {
                var payload = envelope.payloads[i];
                if (payload == null)
                {
                    EditorGUILayout.HelpBox($"Payload #{i + 1} is null.", MessageType.Warning);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(payload.key))
                {
                    EditorGUILayout.HelpBox($"Payload #{i + 1} has empty key.", MessageType.Warning);
                    continue;
                }

                if (!keys.Add(payload.key))
                {
                    EditorGUILayout.HelpBox($"Duplicate payload key: {payload.key}", MessageType.Warning);
                }
            }
        }

        private string DrawScrollableTextArea(string value, ref Vector2 scroll, float height)
        {
            var rect = EditorGUILayout.GetControlRect(false, Mathf.Clamp(height, MinJsonHeight, MaxJsonHeight), GUILayout.ExpandWidth(true));
            scroll = GUI.BeginScrollView(rect, scroll, new Rect(0f, 0f, Mathf.Max(rect.width - 18f, 1f), Mathf.Max(height - 4f, jsonTextAreaStyle.CalcHeight(new GUIContent(value), Mathf.Max(rect.width - 22f, 1f)))));
            var next = EditorGUI.TextArea(new Rect(0f, 0f, Mathf.Max(rect.width - 22f, 1f), Mathf.Max(height - 4f, jsonTextAreaStyle.CalcHeight(new GUIContent(value), Mathf.Max(rect.width - 22f, 1f)))), value, jsonTextAreaStyle);
            GUI.EndScrollView();
            return next;
        }

        private void LoadFromFile()
        {
            var path = GetFilePath();
            if (!File.Exists(path))
            {
                SetStatus($"File not found: {path}", MessageType.Warning);
                return;
            }

            try
            {
                envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
                EnsureEnvelope();
                SyncRawJsonFromEnvelope();
                SetStatus($"Loaded file: {path}", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus($"Failed to load file: {exception.Message}", MessageType.Error);
            }
        }

        private void SaveToFile()
        {
            try
            {
                EnsureEnvelope();
                TouchEnvelope();
                Directory.CreateDirectory(UnityEngine.Application.persistentDataPath);
                File.WriteAllText(GetFilePath(), JsonUtility.ToJson(envelope, true));
                SyncRawJsonFromEnvelope();
                RefreshFiles();
                SetStatus($"Saved file: {GetFilePath()}", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus($"Failed to save file: {exception.Message}", MessageType.Error);
            }
        }

        private void DeleteFile()
        {
            var path = GetFilePath();
            if (!File.Exists(path))
            {
                SetStatus($"File not found: {path}", MessageType.Warning);
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete Save File", $"Delete save file?\n\n{path}", "Delete", "Cancel"))
            {
                return;
            }

            File.Delete(path);
            RefreshFiles();
            SetStatus($"Deleted file: {path}", MessageType.Info);
        }

        private void LoadFromPlayerPrefs()
        {
            var key = NormalizePlayerPrefsKey(playerPrefsKey);
            if (!PlayerPrefs.HasKey(key))
            {
                SetStatus($"PlayerPrefs key not found: {key}", MessageType.Warning);
                return;
            }

            try
            {
                envelope = JsonUtility.FromJson<SaveEnvelope>(PlayerPrefs.GetString(key));
                EnsureEnvelope();
                SyncRawJsonFromEnvelope();
                SetStatus($"Loaded PlayerPrefs key: {key}", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus($"Failed to load PlayerPrefs: {exception.Message}", MessageType.Error);
            }
        }

        private void SaveToPlayerPrefs()
        {
            try
            {
                EnsureEnvelope();
                TouchEnvelope();
                var key = NormalizePlayerPrefsKey(playerPrefsKey);
                PlayerPrefs.SetString(key, JsonUtility.ToJson(envelope));
                PlayerPrefs.Save();
                SyncRawJsonFromEnvelope();
                SetStatus($"Saved PlayerPrefs key: {key}", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus($"Failed to save PlayerPrefs: {exception.Message}", MessageType.Error);
            }
        }

        private void DeletePlayerPrefs()
        {
            var key = NormalizePlayerPrefsKey(playerPrefsKey);
            if (!PlayerPrefs.HasKey(key))
            {
                SetStatus($"PlayerPrefs key not found: {key}", MessageType.Warning);
                return;
            }

            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            SetStatus($"Deleted PlayerPrefs key: {key}", MessageType.Info);
        }

        private void ApplyRawJsonToEnvelope()
        {
            try
            {
                envelope = JsonUtility.FromJson<SaveEnvelope>(rawEnvelopeJson);
                EnsureEnvelope();
                SetStatus("Applied raw json to envelope.", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus($"Failed to parse raw json: {exception.Message}", MessageType.Error);
            }
        }

        private void SyncRawJsonFromEnvelope()
        {
            envelope ??= new SaveEnvelope();
            envelope.schemaVersion = envelope.schemaVersion > 0 ? envelope.schemaVersion : 1;
            envelope.payloads ??= new List<SavePayloadData>();
            rawEnvelopeJson = JsonUtility.ToJson(envelope, true);
        }

        private void RefreshFiles()
        {
            files.Clear();

            if (!Directory.Exists(UnityEngine.Application.persistentDataPath))
            {
                return;
            }

            var paths = Directory.GetFiles(UnityEngine.Application.persistentDataPath, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(paths, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < paths.Length; i++)
            {
                files.Add(Path.GetFileName(paths[i]));
            }
        }

        private void EnsureEnvelope()
        {
            envelope ??= new SaveEnvelope();
            envelope.schemaVersion = envelope.schemaVersion > 0 ? envelope.schemaVersion : 1;
            envelope.payloads ??= new List<SavePayloadData>();
            EnsurePayloadScrollCount(envelope.payloads.Count);
            if (string.IsNullOrWhiteSpace(rawEnvelopeJson))
            {
                rawEnvelopeJson = JsonUtility.ToJson(envelope, true);
            }
        }

        private void EnsurePayloadScrollCount(int count)
        {
            while (payloadJsonScrollPositions.Count < count)
            {
                payloadJsonScrollPositions.Add(Vector2.zero);
            }

            while (payloadJsonScrollPositions.Count > count)
            {
                payloadJsonScrollPositions.RemoveAt(payloadJsonScrollPositions.Count - 1);
            }
        }

        private void TouchEnvelope()
        {
            EnsureEnvelope();
            envelope.updatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static string FormatUnixMs(long unixMs, bool utc)
        {
            if (unixMs <= 0)
            {
                return "Not set";
            }

            try
            {
                var value = DateTimeOffset.FromUnixTimeMilliseconds(unixMs);
                value = utc ? value.ToUniversalTime() : value.ToLocalTime();
                return value.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
            }
            catch (ArgumentOutOfRangeException)
            {
                return "Invalid timestamp";
            }
        }

        private string GetFilePath()
        {
            fileName = NormalizeFileName(fileName);
            return Path.Combine(UnityEngine.Application.persistentDataPath, fileName);
        }

        private static string NormalizeFileName(string value)
        {
            value = string.IsNullOrWhiteSpace(value) ? SaveStorageDefaults.FileName : value.Trim();
            value = Path.GetFileName(value.Replace('\\', '/'));
            return string.IsNullOrWhiteSpace(Path.GetExtension(value)) ? $"{value}.json" : value;
        }

        private static string NormalizePlayerPrefsKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? SaveStorageDefaults.PlayerPrefsKey : value.Trim();
        }

        private static string BuildPrefsKey(string name)
        {
            return $"{EditorPrefsPrefix}{UnityEngine.Application.dataPath.GetHashCode()}.{name}";
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
            Repaint();
        }

        private void EnsureStyles()
        {
            jsonTextAreaStyle ??= new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true
            };
        }
    }
}
