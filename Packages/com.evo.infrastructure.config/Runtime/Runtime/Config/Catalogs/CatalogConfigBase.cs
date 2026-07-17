using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.Config;
using UnityEngine;

namespace Evo.Infrastructure.Runtime.Config.Catalogs
{
    public interface ICatalogItemWithId
    {
        string Id { get; }
    }

    public interface IConfigCatalog<out TItem> where TItem : UnityEngine.Object
    {
        IReadOnlyList<TItem> Items { get; }
    }

    public interface ICatalogEditorMetadata
    {
        Type ItemType { get; }
        IReadOnlyList<Type> GetCreatableItemTypes();
        string CreateAssetDirectory { get; }
        string BuildAssetBaseName(Type itemType);
        string BuildSuggestedId(string assetName, Type itemType);
        CatalogValidationResult ValidateCatalog();
    }

    public sealed class CatalogValidationResult
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public bool HasErrors => _errors.Count > 0;
        public bool HasWarnings => _warnings.Count > 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _errors.Add(message);
            }
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _warnings.Add(message);
            }
        }
    }

    public abstract class CatalogConfigBase<TItem> : ScriptableObject, IGameConfig, IConfigCatalog<TItem>, ICatalogEditorMetadata
        where TItem : UnityEngine.Object
    {
        [NonSerialized] private CatalogValidationResult _lastValidation;
        [NonSerialized] private int _lastRemovedNullCount;

        protected abstract List<TItem> MutableItems { get; set; }

        public IReadOnlyList<TItem> Items => MutableItems;
        public Type ItemType => typeof(TItem);
        public virtual string CreateAssetDirectory => "Assets/_Project/Configs";

        protected virtual string DefaultAssetBaseName => $"new_{NormalizeId(typeof(TItem).Name)}";

        public CatalogValidationResult ValidateCatalog()
        {
            var result = new CatalogValidationResult();
            var items = MutableItems;
            if (items == null)
            {
                result.AddError("Items list is null.");
                return result;
            }

            var duplicateMap = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null)
                {
                    result.AddError($"Null entry at index {i}.");
                    continue;
                }

                if (item is not ICatalogItemWithId)
                {
                    result.AddError(
                        $"Item at index {i} ({item.name}) must implement {nameof(ICatalogItemWithId)}. " +
                        "Reflected Id lookup is not supported in player builds.");
                    continue;
                }

                var id = GetItemId(item)?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id))
                {
                    result.AddError($"Empty id at index {i} ({item.name}).");
                    continue;
                }

                if (!duplicateMap.TryGetValue(id, out var indices))
                {
                    indices = new List<int>();
                    duplicateMap[id] = indices;
                }

                indices.Add(i);
            }

            foreach (var pair in duplicateMap)
            {
                if (pair.Value.Count <= 1)
                {
                    continue;
                }

                result.AddError($"Duplicate id '{pair.Key}' at indices: {string.Join(", ", pair.Value)}.");
            }

            if (_lastRemovedNullCount > 0)
            {
                result.AddWarning($"Auto-cleaned {_lastRemovedNullCount} null entr{(_lastRemovedNullCount == 1 ? "y" : "ies")} on last validation.");
            }

            return result;
        }

        protected void AssignItems(List<TItem> valueItems)
        {
            MutableItems = valueItems == null
                ? new List<TItem>()
                : valueItems.FindAll(item => item != null);
        }

        public virtual IReadOnlyList<Type> GetCreatableItemTypes()
        {
            if (!typeof(TItem).IsAbstract && typeof(ScriptableObject).IsAssignableFrom(typeof(TItem)))
            {
                return new[] { typeof(TItem) };
            }

            return Array.Empty<Type>();
        }

        public virtual string BuildAssetBaseName(Type itemType)
        {
            return DefaultAssetBaseName;
        }

        public virtual string BuildSuggestedId(string assetName, Type itemType)
        {
            return NormalizeId(assetName);
        }

        protected virtual string GetItemId(TItem item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            if (item is ICatalogItemWithId withId)
            {
                return withId.Id ?? string.Empty;
            }

            return string.Empty;
        }

        protected virtual void OnValidate()
        {
            MutableItems ??= new List<TItem>();
            _lastRemovedNullCount = MutableItems.RemoveAll(item => item == null);
            _lastValidation = ValidateCatalog();
        }

        protected string BuildValidationSummary()
        {
            _lastValidation ??= ValidateCatalog();
            if (!_lastValidation.HasErrors && !_lastValidation.HasWarnings)
            {
                return "No issues found.";
            }

            var messages = new List<string>();
            for (var i = 0; i < _lastValidation.Errors.Count; i++)
            {
                messages.Add($"Error: {_lastValidation.Errors[i]}");
            }

            for (var i = 0; i < _lastValidation.Warnings.Count; i++)
            {
                messages.Add($"Warning: {_lastValidation.Warnings[i]}");
            }

            return string.Join("\n", messages);
        }

        protected static string NormalizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "new_item";
            }

            var chars = value.Trim().ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                chars[i] = char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_';
            }

            return new string(chars);
        }
    }
}
