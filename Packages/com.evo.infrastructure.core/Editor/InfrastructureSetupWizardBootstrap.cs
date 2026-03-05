#if UNITY_EDITOR
using UnityEditor;

namespace Evo.Infrastructure.Core.Editor
{
    [InitializeOnLoad]
    internal static class InfrastructureSetupWizardBootstrap
    {
        private const string PromptKey = "Evo.Infrastructure.Core.SetupPromptShown";

        static InfrastructureSetupWizardBootstrap()
        {
            EditorApplication.delayCall += TryShowPrompt;
        }

        private static void TryShowPrompt()
        {
            if (EditorPrefs.GetBool(PromptKey, false))
            {
                return;
            }

            var open = EditorUtility.DisplayDialog(
                "Evo Infrastructure",
                "Core package installed. Open setup wizard now?",
                "Open Wizard",
                "Later");

            EditorPrefs.SetBool(PromptKey, true);
            if (open)
            {
                InfrastructureSetupWizardWindow.OpenWindow();
            }
        }
    }
}
#endif
