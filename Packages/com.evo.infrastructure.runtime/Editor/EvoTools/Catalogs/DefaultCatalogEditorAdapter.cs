using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Evo.Infrastructure.Runtime.Config.Catalogs;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    public sealed class ReflectionCatalogItemKeyProvider : ICatalogItemKeyProvider
    {
        private static readonly string[] PropertyNames = { "Id", "CueName", "Key", "Name" };
        private static readonly string[] FieldNames = { "id", "cueName", "key", "name" };

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

            for (var i = 0; i < PropertyNames.Length; i++)
            {
                var value = ResolveStringProperty(item, PropertyNames[i]);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return new CatalogItemKey(value.Trim(), i == 0 ? CatalogItemKeySource.Explicit : CatalogItemKeySource.Custom, true);
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
            return item != null && FindWritableStringMember(item.GetType()) != null;
        }

        public bool TrySetKey(UnityEngine.Object item, string value)
        {
            if (item == null)
            {
                return false;
            }

            var member = FindWritableStringMember(item.GetType());
            switch (member)
            {
                case PropertyInfo property:
                    property.SetValue(item, value ?? string.Empty);
                    return true;
                case FieldInfo field:
                    field.SetValue(item, value ?? string.Empty);
                    return true;
                default:
                    return false;
            }
        }

        private static string ResolveStringProperty(object item, string propertyName)
        {
            var property = item.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return property?.PropertyType == typeof(string) ? property.GetValue(item) as string ?? string.Empty : string.Empty;
        }

        private static MemberInfo FindWritableStringMember(Type type)
        {
            for (var i = 0; i < PropertyNames.Length; i++)
            {
                var property = type.GetProperty(PropertyNames[i], BindingFlags.Instance | BindingFlags.Public);
                if (property?.PropertyType == typeof(string) && property.CanWrite)
                {
                    return property;
                }
            }

            var current = type;
            while (current != null && current != typeof(object))
            {
                for (var i = 0; i < FieldNames.Length; i++)
                {
                    var field = current.GetField(FieldNames[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field?.FieldType == typeof(string) && !field.IsInitOnly)
                    {
                        return field;
                    }
                }

                current = current.BaseType;
            }

            return null;
        }
    }

    internal sealed class DefaultCatalogEditorAdapter : ICatalogEditorAdapter
    {
        private static readonly ReflectionCatalogItemKeyProvider DefaultKeyProvider = new();

        private readonly List<CatalogCategoryDescriptor> _categories;

        private DefaultCatalogEditorAdapter(ScriptableObject catalogAsset, List<CatalogCategoryDescriptor> categories)
        {
            CatalogAsset = catalogAsset;
            Title = $"{catalogAsset.name} Catalog";
            _categories = categories;
        }

        public ScriptableObject CatalogAsset { get; }
        public string Title { get; }
        public IReadOnlyList<CatalogCategoryDescriptor> Categories => _categories;

        public CatalogValidationResult Validate()
        {
            return CatalogEditorValidation.Validate(this);
        }

        public static bool TryCreate(ScriptableObject catalogAsset, out ICatalogEditorAdapter adapter)
        {
            adapter = null;
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
                    GetCreatableTypes = GetCreatableTypes,
                    KeyProvider = DefaultKeyProvider
                });
            }

            adapter = new DefaultCatalogEditorAdapter(catalogAsset, categories);
            return true;
        }

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
