#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;

namespace Evo.Infrastructure.Core.Editor.Setup
{
    internal static class SdkAssemblyDetector
    {
        public static bool IsAvailable(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return false;
            }

            if (CompilationPipeline.GetAssemblies()
                .Any(assembly => string.Equals(assembly.name, assemblyName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return AssetDatabase.FindAssets(assemblyName)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Any(path => MatchesPrecompiledAssemblyPath(path, assemblyName));
        }

        internal static bool MatchesPrecompiledAssemblyPath(string assetPath, string assemblyName)
        {
            return !string.IsNullOrWhiteSpace(assetPath) &&
                   string.Equals(Path.GetExtension(assetPath), ".dll", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Path.GetFileNameWithoutExtension(assetPath), assemblyName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
