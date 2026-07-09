using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.Services.Save
{
    [Serializable]
    public sealed class SaveBackendSelectionPolicy
    {
        public SaveBackendUsage defaultUsage = SaveBackendUsage.Enabled;
        public List<SaveBackendSelectionRule> backendRules = new();

        public static SaveBackendSelectionPolicy AllEnabled()
        {
            return new SaveBackendSelectionPolicy();
        }

        public static SaveBackendSelectionPolicy Only(params string[] backendIds)
        {
            var policy = new SaveBackendSelectionPolicy
            {
                defaultUsage = SaveBackendUsage.Disabled
            };

            if (backendIds == null)
            {
                return policy;
            }

            for (var i = 0; i < backendIds.Length; i++)
            {
                policy.SetUsage(backendIds[i], SaveBackendUsage.Enabled);
            }

            return policy;
        }

        public static SaveBackendSelectionPolicy CloudPrimaryWithLocalLoadFallback(string cloudBackendId)
        {
            var policy = new SaveBackendSelectionPolicy
            {
                defaultUsage = SaveBackendUsage.Disabled
            };
            policy.SetUsage(cloudBackendId, SaveBackendUsage.Enabled);
            policy.SetUsage("file", SaveBackendUsage.LoadOnly);
            policy.SetUsage("prefs", SaveBackendUsage.LoadOnly);
            return policy;
        }

        public SaveBackendSelectionPolicy SetUsage(string backendId, SaveBackendUsage usage)
        {
            if (string.IsNullOrWhiteSpace(backendId))
            {
                return this;
            }

            backendRules ??= new List<SaveBackendSelectionRule>();
            for (var i = 0; i < backendRules.Count; i++)
            {
                var rule = backendRules[i];
                if (rule == null || !string.Equals(rule.backendId, backendId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                rule.usage = usage;
                return this;
            }

            backendRules.Add(new SaveBackendSelectionRule
            {
                backendId = backendId,
                usage = usage
            });
            return this;
        }

        public bool CanLoad(ISaveBackend backend)
        {
            return Allows(backend, isLoad: true);
        }

        public bool CanSave(ISaveBackend backend)
        {
            return Allows(backend, isLoad: false);
        }

        private bool Allows(ISaveBackend backend, bool isLoad)
        {
            if (backend == null || !backend.IsAvailable)
            {
                return false;
            }

            var usage = ResolveUsage(backend.BackendId);
            return usage == SaveBackendUsage.Enabled ||
                   isLoad && usage == SaveBackendUsage.LoadOnly ||
                   !isLoad && usage == SaveBackendUsage.SaveOnly;
        }

        private SaveBackendUsage ResolveUsage(string backendId)
        {
            if (backendRules == null || string.IsNullOrWhiteSpace(backendId))
            {
                return defaultUsage;
            }

            for (var i = 0; i < backendRules.Count; i++)
            {
                var rule = backendRules[i];
                if (rule == null || string.IsNullOrWhiteSpace(rule.backendId))
                {
                    continue;
                }

                if (string.Equals(rule.backendId, backendId, StringComparison.OrdinalIgnoreCase))
                {
                    return rule.usage;
                }
            }

            return defaultUsage;
        }
    }
}
