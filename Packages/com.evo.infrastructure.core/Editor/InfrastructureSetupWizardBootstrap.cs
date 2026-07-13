#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Core.Editor
{
    [InitializeOnLoad]
    internal static class InfrastructureSetupWizardBootstrap
    {
        private const string PromptKeyPrefix = "Evo.Infrastructure.Core.SetupPromptShown.";

        static InfrastructureSetupWizardBootstrap()
        {
            EditorApplication.delayCall += TryShowPrompt;
        }

        private static void TryShowPrompt()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            var promptKey = PromptKeyPrefix + Application.dataPath.GetHashCode() + "." + GetPackageStamp();
            if (EditorPrefs.GetBool(promptKey, false))
            {
                return;
            }

            var open = EditorUtility.DisplayDialog(
                "Evo Infrastructure",
                "Core package installed. Open setup wizard now?",
                "Open Wizard",
                "Later");

            EditorPrefs.SetBool(promptKey, true);
            if (open)
            {
                InfrastructureSetupWizardWindow.OpenWindow();
            }
        }

        private static long GetPackageStamp()
        {
            var packageJsonPath = Path.GetFullPath("Packages/com.evo.infrastructure.core/package.json");
            return File.Exists(packageJsonPath)
                ? File.GetLastWriteTimeUtc(packageJsonPath).Ticks
                : 0L;
        }
    }
}
#endif
