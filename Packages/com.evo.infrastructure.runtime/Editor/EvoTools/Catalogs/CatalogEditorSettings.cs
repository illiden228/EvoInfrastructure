using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    [FilePath("ProjectSettings/EvoCatalogEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class CatalogEditorSettings : ScriptableSingleton<CatalogEditorSettings>
    {
        [SerializeField] private List<DirectoryEntry> directories = new();

        public string GetDirectory(string key, string fallback)
        {
            for (var i = 0; i < directories.Count; i++)
            {
                var entry = directories[i];
                if (entry != null && string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    return NormalizeDirectory(entry.Value, fallback);
                }
            }

            return NormalizeDirectory(fallback, "Assets");
        }

        public void SetDirectory(string key, string value, string fallback)
        {
            for (var i = 0; i < directories.Count; i++)
            {
                var entry = directories[i];
                if (entry == null || !string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    continue;
                }

                entry.Value = NormalizeDirectory(value, fallback);
                Save(true);
                return;
            }

            directories.Add(new DirectoryEntry(key, NormalizeDirectory(value, fallback)));
            Save(true);
        }

        private static string NormalizeDirectory(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Replace('\\', '/').TrimEnd('/');
        }

        [Serializable]
        private sealed class DirectoryEntry
        {
            [SerializeField] private string key;
            [SerializeField] private string value;

            public DirectoryEntry(string key, string value)
            {
                this.key = key;
                this.value = value;
            }

            public string Key => key;

            public string Value
            {
                get => value;
                set => this.value = value;
            }
        }
    }
}
