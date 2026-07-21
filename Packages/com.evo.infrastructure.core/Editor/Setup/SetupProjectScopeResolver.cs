#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
            var componentTypes = TypeCache.GetTypesDerivedFrom<Component>();
            for (var i = 0; i < componentTypes.Count; i++)
            {
                var type = componentTypes[i];
                if (type != null && IsSupported(type))
                {
                    return type;
                }
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
