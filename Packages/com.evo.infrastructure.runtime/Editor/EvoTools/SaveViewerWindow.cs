using System;
using System.Collections.Generic;
using System.IO;
using _Project.Scripts.Infrastructure.Services.Save;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.Editor.EvoTools
{
    public sealed class SaveViewerWindow : EditorWindow
    {
        private const string MenuPath = "EvoTools/Save Viewer";
        private const string EditorPrefsPrefix = "EvoTools.SaveViewer.";

        [SerializeField] private string fileName = SaveStorageDefaults.FileName;
        [SerializeField] private string playerPrefsKey = SaveStorageDefaults.PlayerPrefsKey;
        [SerializeField] private SaveEnvelope envelope;
        [SerializeField] private string rawEnvelopeJson = string.Empty;
        [SerializeField] private Vector2 scrollPosition;
        [SerializeField] private Vector2 fileListScrollPosition;

        private readonly List<string> files = new();
        private GUIStyle jsonTextAreaStyle;
        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.Info;

        [MenuItem(MenuPath, false, 81)]
        public static void Open()
        {
            var window = GetWindow<SaveViewerWindow>("Save Viewer");
            window.minSize = new Vector2(620f, 520f);
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
                EditorUtility.RevealInFinder(Application.persistentDataPath);
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
            if (GUILayout.Button("Add Payload", GUILayout.Width(120f)))
            {
                envelope.payloads.Add(new SavePayloadData { key = "default", version = 1, json = "{}" });
                SyncRawJsonFromEnvelope();
            }

            EditorGUILayout.EndHorizontal();

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
                payload.json = EditorGUILayout.TextArea(payload.json ?? string.Empty, jsonTextAreaStyle, GUILayout.MinHeight(72f));
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawRawJson()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Raw Envelope Json", EditorStyles.boldLabel);
            rawEnvelopeJson = EditorGUILayout.TextArea(rawEnvelopeJson ?? string.Empty, jsonTextAreaStyle, GUILayout.MinHeight(140f));

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
                Directory.CreateDirectory(Application.persistentDataPath);
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
            EnsureEnvelope();
            rawEnvelopeJson = JsonUtility.ToJson(envelope, true);
        }

        private void RefreshFiles()
        {
            files.Clear();

            if (!Directory.Exists(Application.persistentDataPath))
            {
                return;
            }

            var paths = Directory.GetFiles(Application.persistentDataPath, "*.json", SearchOption.TopDirectoryOnly);
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
            if (string.IsNullOrWhiteSpace(rawEnvelopeJson))
            {
                SyncRawJsonFromEnvelope();
            }
        }

        private void TouchEnvelope()
        {
            EnsureEnvelope();
            envelope.updatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private string GetFilePath()
        {
            fileName = NormalizeFileName(fileName);
            return Path.Combine(Application.persistentDataPath, fileName);
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
            return $"{EditorPrefsPrefix}{Application.dataPath.GetHashCode()}.{name}";
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
                wordWrap = false
            };
        }
    }
}
