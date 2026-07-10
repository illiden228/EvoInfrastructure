#if UNITY_EDITOR
using System;
using System.Linq;

namespace Evo.Infrastructure.Core.Editor.Setup
{
    internal static class SetupProjectScopeResolver
    {
        internal static readonly string[] SupportedNames =
        {
            "Game.Runtime.EntryPoint.RuntimeProjectLifetimeScope",
            "Game.Runtime.Bootstrap.RuntimeProjectLifetimeScope",
            "RuntimeProjectLifetimeScope"
        };

        public static Type Resolve()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var name in SupportedNames)
            {
                var type = assembly.GetType(name, false);
                if (type != null) return type;
            }
            return null;
        }

        public static bool IsSupported(Type type) => type != null && IsSupportedName(type.FullName);

        public static bool IsSupportedName(string fullName) =>
            !string.IsNullOrWhiteSpace(fullName) && SupportedNames.Contains(fullName);

        public static bool IsLegacyProjectOwnedScope(Type type) =>
            type != null && type.Name == "ProjectLifetimeScope" && !IsSupported(type);
    }
}
#endif
