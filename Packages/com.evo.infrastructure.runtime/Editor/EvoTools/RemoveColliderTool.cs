using UnityEngine;
using UnityEditor;
using EvoDebug = _Project.Scripts.Infrastructure.Services.Debug.EvoDebug;

public static class RemoveColliderTool
{
    private const string SOURCE = nameof(RemoveColliderTool);

    [MenuItem("EvoTools/Remove Colliders from Prefab")]
    public static void RemoveCollidersFromSelectedPrefab()
    {
        if (Selection.activeObject == null)
        {
            EvoDebug.LogWarning("Пожалуйста, выберите префаб в окне Project.", SOURCE);
            return;
        }

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (string.IsNullOrEmpty(path) || !PrefabUtility.IsPartOfPrefabAsset(Selection.activeObject))
        {
            EvoDebug.LogWarning("Выбранный объект не является префабом. Выберите файл префаба в окне Project.", SOURCE);
            return;
        }

        GameObject prefabRoot = null;

        try
        {
            prefabRoot = PrefabUtility.LoadPrefabContents(path);

            if (prefabRoot == null)
            {
                EvoDebug.LogError($"Не удалось загрузить префаб: {path}", SOURCE);
                return;
            }

            Collider[] colliders = prefabRoot.GetComponentsInChildren<Collider>(true);
            int removedCount = 0;

            foreach (Collider collider in colliders)
            {
                Undo.DestroyObjectImmediate(collider);
                removedCount++;
            }

            bool saved = PrefabUtility.SaveAsPrefabAsset(prefabRoot, path, out bool success);
            if (!saved || !success)
            {
                EvoDebug.LogError($"Не удалось сохранить префаб после удаления MeshCollider: {path}", SOURCE);
                return;
            }

            EvoDebug.Log($"Удалено {removedCount} компонентов MeshCollider из префаба: {Selection.activeObject.name} ({path})", SOURCE);
        }
        finally
        {
            if (prefabRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    [MenuItem("EvoTools/Remove Colliders from Prefab", true)]
    public static bool ValidateRemoveMeshCollidersFromSelectedPrefab()
    {
        return Selection.activeObject != null &&
               !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject)) &&
               PrefabUtility.IsPartOfPrefabAsset(Selection.activeObject);
    }
}
