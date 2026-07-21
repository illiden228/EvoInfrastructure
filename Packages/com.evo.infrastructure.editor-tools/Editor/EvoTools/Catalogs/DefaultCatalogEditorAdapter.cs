using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Evo.Infrastructure.Runtime.Config.Catalogs;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    public class SerializedCatalogItemKeyProvider : ICatalogItemKeyProvider
    {
        private static readonly string[] FieldNames = { "id", "platformId", "adapterId", "cueName", "key", "name" };

        public CatalogItemKey GetKey(UnityEngine.Object item, int index)
        {
            if (item == null)
            {
                return new CatalogItemKey(string.Empty, CatalogItemKeySource.Missing, false);
            }

            if (item is ICatalogItemWithId withId && !string.IsNullOrWhiteSpace(withId.Id))
            {
                return new CatalogItemKey(withId.Id.Trim(), CatalogItemKeySource.Explicit, true);
            }

            var serialized = new SerializedObject(item);
            for (var i = 0; i < FieldNames.Length; i++)
            {
                var property = serialized.FindProperty(FieldNames[i]);
                if (property?.propertyType == SerializedPropertyType.String &&
                    !string.IsNullOrWhiteSpace(property.stringValue))
                {
                    return new CatalogItemKey(
                        property.stringValue.Trim(),
                        i == 0 ? CatalogItemKeySource.Explicit : CatalogItemKeySource.Custom,
                        true);
                }
            }

            if (!string.IsNullOrWhiteSpace(item.name))
            {
                return new CatalogItemKey(item.name.Trim(), CatalogItemKeySource.AssetName, true);
            }

            return new CatalogItemKey(index.ToString(), CatalogItemKeySource.Index, false);
        }

        public string GetDisplayName(UnityEngine.Object item, int index)
        {
            if (item == null)
            {
                return $"Missing item #{index}";
            }

            var key = GetKey(item, index);
            if (!string.IsNullOrWhiteSpace(key.Value))
            {
                return key.Value;
            }

            return string.IsNullOrWhiteSpace(item.name) ? $"{item.GetType().Name} #{index}" : item.name;
        }

        public bool CanSetKey(UnityEngine.Object item)
        {
            return item != null && FindSerializedStringProperty(new SerializedObject(item)) != null;
        }

        public bool TrySetKey(UnityEngine.Object item, string value)
        {
            if (item == null)
            {
                return false;
            }

            var serialized = new SerializedObject(item);
            serialized.UpdateIfRequiredOrScript();
            var property = FindSerializedStringProperty(serialized);
            if (property == null)
            {
                return false;
            }

            property.stringValue = value ?? string.Empty;
            serialized.ApplyModifiedProperties();
            return true;
        }

        private static SerializedProperty FindSerializedStringProperty(SerializedObject serialized)
        {
            if (serialized == null)
            {
                return null;
            }

            for (var i = 0; i < FieldNames.Length; i++)
            {
                var property = serialized.FindProperty(FieldNames[i]);
                if (property?.propertyType == SerializedPropertyType.String)
                {
                    return property;
                }
            }

            return null;
        }
    }

    [Obsolete("Use SerializedCatalogItemKeyProvider. The compatibility name no longer uses reflection.")]
    public sealed class ReflectionCatalogItemKeyProvider : SerializedCatalogItemKeyProvider
    {
    }

    internal sealed class DefaultCatalogEditorAdapter : ICatalogEditorAdapter
    {
        private static readonly SerializedCatalogItemKeyProvider DefaultKeyProvider = new();

        private readonly List<CatalogCategoryDescriptor> _categories;
        private readonly Func<CatalogValidationResult> _validate;

        private DefaultCatalogEditorAdapter(
            ScriptableObject catalogAsset,
            List<CatalogCategoryDescriptor> categories,
            Func<CatalogValidationResult> validate = null)
        {
            CatalogAsset = catalogAsset;
            Title = $"{catalogAsset.name} Catalog";
            _categories = categories;
            _validate = validate;
        }

        public ScriptableObject CatalogAsset { get; }
        public string Title { get; }
        public IReadOnlyList<CatalogCategoryDescriptor> Categories => _categories;

        public CatalogValidationResult Validate()
        {
            var result = CatalogEditorValidation.Validate(this);
            var customValidation = _validate?.Invoke();
            if (customValidation == null)
            {
                return result;
            }

            for (var i = 0; i < customValidation.Errors.Count; i++)
            {
                AddUniqueError(result, customValidation.Errors[i]);
            }

            for (var i = 0; i < customValidation.Warnings.Count; i++)
            {
                AddUniqueWarning(result, customValidation.Warnings[i]);
            }

            return result;
        }

        public static bool TryCreate(ScriptableObject catalogAsset, out ICatalogEditorAdapter adapter)
        {
            adapter = null;
            if (catalogAsset is ICatalogEditorMetadata metadata && TryCreateMetadata(catalogAsset, metadata, out adapter))
            {
                return true;
            }

            var fields = FindListFields(catalogAsset.GetType());
            if (fields.Count == 0)
            {
                return false;
            }

            var categories = new List<CatalogCategoryDescriptor>(fields.Count);
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var itemType = field.FieldType.GetGenericArguments()[0];
                categories.Add(new CatalogCategoryDescriptor
                {
                    Id = field.Name,
                    Name = fields.Count == 1 ? "All" : ObjectNames.NicifyVariableName(field.Name),
                    ItemType = itemType,
                    DefaultCreateDirectory = ResolveDefaultDirectory(catalogAsset),
                    Contains = _ => true,
                    GetMutableList = () => field.GetValue(catalogAsset) as IList,
                    BuildAssetBaseName = BuildDefaultAssetBaseName,
                    BuildSuggestedId = (assetName, _) => NormalizeId(assetName),
                    GetCreatableTypes = GetCreatableTypes,
                    KeyProvider = DefaultKeyProvider
                });
            }

            adapter = new DefaultCatalogEditorAdapter(catalogAsset, categories);
            return true;
        }

        private static bool TryCreateMetadata(
            ScriptableObject catalogAsset,
            ICatalogEditorMetadata metadata,
            out ICatalogEditorAdapter adapter)
        {
            adapter = null;
            if (metadata == null || metadata.ItemType == null)
            {
                return false;
            }

            if (catalogAsset is not ICatalogEditorItemsProvider itemsProvider ||
                itemsProvider.MutableEditorItems == null)
            {
                return false;
            }

            var categories = new List<CatalogCategoryDescriptor>
            {
                new()
                {
                    Id = "items",
                    Name = "All",
                    ItemType = metadata.ItemType,
                    DefaultCreateDirectory = string.IsNullOrWhiteSpace(metadata.CreateAssetDirectory)
                        ? ResolveDefaultDirectory(catalogAsset)
                        : metadata.CreateAssetDirectory,
                    Contains = _ => true,
                    GetMutableList = () => itemsProvider.MutableEditorItems,
                    BuildAssetBaseName = metadata.BuildAssetBaseName,
                    BuildSuggestedId = metadata.BuildSuggestedId,
                    GetCreatableTypes = itemType => ResolveCreatableTypes(metadata, itemType),
                    KeyProvider = DefaultKeyProvider
                }
            };

            adapter = new DefaultCatalogEditorAdapter(catalogAsset, categories, metadata.ValidateCatalog);
            return true;
        }

        // Reflection is intentionally limited to the legacy fallback for project-owned catalogs
        // that do not implement ICatalogEditorMetadata. Infrastructure catalogs use typed adapters.
        private static List<FieldInfo> FindListFields(Type catalogType)
        {
            var result = new List<FieldInfo>();
            var current = catalogType;
            while (current != null && current != typeof(ScriptableObject) && current != typeof(object))
            {
                var fields = current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (var i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(List<>))
                    {
                        continue;
                    }

                    var itemType = field.FieldType.GetGenericArguments()[0];
                    if (typeof(ScriptableObject).IsAssignableFrom(itemType))
                    {
                        result.Add(field);
                    }
                }

                current = current.BaseType;
            }

            result.Reverse();
            return result;
        }

        internal static IReadOnlyList<Type> GetCreatableTypes(Type itemType)
        {
            var result = new List<Type>();
            if (itemType == null)
            {
                return result;
            }

            if (!itemType.IsAbstract && typeof(ScriptableObject).IsAssignableFrom(itemType))
            {
                result.Add(itemType);
            }

            var derived = TypeCache.GetTypesDerivedFrom(itemType);
            for (var i = 0; i < derived.Count; i++)
            {
                var type = derived[i];
                if (type == null || type.IsAbstract || type.IsGenericType || type.ContainsGenericParameters)
                {
                    continue;
                }

                if (typeof(ScriptableObject).IsAssignableFrom(type) && !result.Contains(type))
                {
                    result.Add(type);
                }
            }

            result.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));
            return result;
        }

        private static IReadOnlyList<Type> ResolveCreatableTypes(ICatalogEditorMetadata metadata, Type itemType)
        {
            var result = new List<Type>();
            var metadataTypes = metadata.GetCreatableItemTypes();
            if (metadataTypes != null && metadataTypes.Count > 0)
            {
                for (var i = 0; i < metadataTypes.Count; i++)
                {
                    var type = metadataTypes[i];
                    if (IsCreatableType(type) && !result.Contains(type))
                    {
                        result.Add(type);
                    }
                }
            }

            var editorTypes = GetCreatableTypes(itemType);
            for (var i = 0; i < editorTypes.Count; i++)
            {
                var type = editorTypes[i];
                if (!result.Contains(type))
                {
                    result.Add(type);
                }
            }

            result.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));
            return result;
        }

        private static bool IsCreatableType(Type type)
        {
            return type != null &&
                   !type.IsAbstract &&
                   !type.IsGenericType &&
                   !type.ContainsGenericParameters &&
                   typeof(ScriptableObject).IsAssignableFrom(type);
        }

        private static string ResolveDefaultDirectory(UnityEngine.Object catalogAsset)
        {
            var path = AssetDatabase.GetAssetPath(catalogAsset);
            if (string.IsNullOrWhiteSpace(path))
            {
                return "Assets";
            }

            var directory = System.IO.Path.GetDirectoryName(path);
            return string.IsNullOrWhiteSpace(directory) ? "Assets" : directory.Replace('\\', '/');
        }

        private static string BuildDefaultAssetBaseName(Type itemType)
        {
            return itemType == null ? "new_item" : $"new_{NormalizeId(itemType.Name.Replace("Config", string.Empty))}";
        }

        private static void AddUniqueError(CatalogValidationResult result, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            for (var i = 0; i < result.Errors.Count; i++)
            {
                if (string.Equals(result.Errors[i], message, StringComparison.Ordinal))
                {
                    return;
                }
            }

            result.AddError(message);
        }

        private static void AddUniqueWarning(CatalogValidationResult result, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            for (var i = 0; i < result.Warnings.Count; i++)
            {
                if (string.Equals(result.Warnings[i], message, StringComparison.Ordinal))
                {
                    return;
                }
            }

            result.AddWarning(message);
        }

        internal static string NormalizeId(string value)
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
