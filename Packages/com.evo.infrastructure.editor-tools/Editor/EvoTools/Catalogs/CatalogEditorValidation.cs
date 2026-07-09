using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Evo.Infrastructure.Runtime.Config.Catalogs;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    public static class CatalogEditorValidation
    {
        public static CatalogValidationResult Validate(ICatalogEditorAdapter adapter)
        {
            var result = new CatalogValidationResult();
            if (adapter == null)
            {
                result.AddError("Catalog adapter is null.");
                return result;
            }

            var categories = adapter.Categories;
            if (categories == null || categories.Count == 0)
            {
                result.AddWarning("Catalog has no editor categories.");
                return result;
            }

            var validatedLists = new HashSet<int>();
            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                var list = category?.GetMutableList?.Invoke();
                if (list == null)
                {
                    result.AddError($"{GetCategoryName(category, i)}: items list is null.");
                    continue;
                }

                var listId = RuntimeHelpers.GetHashCode(list);
                if (!validatedLists.Add(listId))
                {
                    continue;
                }

                ValidateList(GetCategoryName(category, i), list, category.KeyProvider, result);
            }

            return result;
        }

        private static void ValidateList(string categoryName, IList list, ICatalogItemKeyProvider keyProvider, CatalogValidationResult result)
        {
            var duplicateMap = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] is not UnityEngine.Object item || item == null)
                {
                    result.AddError($"{categoryName}: null entry at index {i}.");
                    continue;
                }

                var key = keyProvider?.GetKey(item, i) ?? new CatalogItemKey(string.Empty, CatalogItemKeySource.Missing, false);
                if (key.Source == CatalogItemKeySource.AssetName)
                {
                    result.AddWarning($"{categoryName}: missing explicit key at index {i} ({item.name}); using asset name fallback.");
                }
                else if (key.Source == CatalogItemKeySource.Index)
                {
                    result.AddWarning($"{categoryName}: missing stable key at index {i} ({item.name}); using index fallback.");
                }
                else if (key.Source == CatalogItemKeySource.Missing || string.IsNullOrWhiteSpace(key.Value))
                {
                    result.AddWarning($"{categoryName}: empty key at index {i} ({item.name}).");
                }

                if (!key.IsStable || string.IsNullOrWhiteSpace(key.Value))
                {
                    continue;
                }

                if (!duplicateMap.TryGetValue(key.Value, out var indices))
                {
                    indices = new List<int>();
                    duplicateMap[key.Value] = indices;
                }

                indices.Add(i);
            }

            foreach (var pair in duplicateMap)
            {
                if (pair.Value.Count > 1)
                {
                    result.AddError($"{categoryName}: duplicate key '{pair.Key}' at indices: {string.Join(", ", pair.Value)}.");
                }
            }
        }

        private static string GetCategoryName(CatalogCategoryDescriptor category, int index)
        {
            return string.IsNullOrWhiteSpace(category?.Name) ? $"Category {index + 1}" : category.Name;
        }
    }
}
