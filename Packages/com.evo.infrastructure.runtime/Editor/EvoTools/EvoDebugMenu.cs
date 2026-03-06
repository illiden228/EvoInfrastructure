using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace _Project.Scripts.Editor.EvoTools
{
    public static class EvoDebugMenu
    {
        private const string DEFINE = "FULL_LOG";
        private const string MENU_PATH = "EvoTools/Debug";

        [MenuItem(MENU_PATH, false, 70)]
        private static void Toggle()
        {
            SetDefine(!HasDefine());
        }

        [MenuItem(MENU_PATH, true, 70)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MENU_PATH, HasDefine());
            return true;
        }

        private static bool HasDefine()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbols(ToNamedBuildTarget(group));
            return ContainsDefine(defines, DEFINE);
        }

        private static void SetDefine(bool enabled)
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbols(ToNamedBuildTarget(group));
            var list = SplitDefines(defines);

            var changed = false;
            if (enabled)
            {
                if (!list.Contains(DEFINE))
                {
                    list.Add(DEFINE);
                    changed = true;
                }
            }
            else
            {
                if (list.Remove(DEFINE))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                var result = string.Join(";", list);
                PlayerSettings.SetScriptingDefineSymbols(ToNamedBuildTarget(group), result);
            }
        }

        private static NamedBuildTarget ToNamedBuildTarget(BuildTargetGroup group)
        {
            return NamedBuildTarget.FromBuildTargetGroup(group);
        }

        private static bool ContainsDefine(string defines, string define)
        {
            if (string.IsNullOrEmpty(defines))
            {
                return false;
            }

            var list = SplitDefines(defines);
            return list.Contains(define);
        }

        private static List<string> SplitDefines(string defines)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(defines))
            {
                return list;
            }

            var tokens = defines.Split(';');
            foreach (var token in tokens)
            {
                var trimmed = token.Trim();
                if (trimmed.Length > 0 && !list.Contains(trimmed))
                {
                    list.Add(trimmed);
                }
            }

            return list;
        }
    }
}
