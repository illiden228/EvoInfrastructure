#if UNITY_EDITOR
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
            var promptKey = PromptKeyPrefix + Application.dataPath.GetHashCode();
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
    }
}
#endif
