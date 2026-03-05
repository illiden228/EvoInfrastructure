using System;
using System.Collections.Generic;
using _Project.Scripts.Infrastructure.Services.Config;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.Analytics.Config
{
    [Serializable]
    public sealed class AnalyticsExtraParameter
    {
        [CatalogDropdown(CatalogDropdownKind.AnalyticsParameterKey)]
        public string Key;
        [Tooltip("@_Project.Scripts.Editor.EvoTools.EvoToolsLocalization.Get(\"analytics.extra_params.value.tooltip\", \"Value for the parameter key. Add tooltip text to EvoTools localization if needed.\")")]
        public string Value;

#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly, HideLabel]
#endif
        private string DisplayName => string.IsNullOrWhiteSpace(Key) ? "<empty>" : Key;

    }

    public abstract class AnalyticsAdapterConfigBase : ScriptableObject
    {
#if ODIN_INSPECTOR
        [Title("Adapter")]
#endif
        [Tooltip("@_Project.Scripts.Editor.EvoTools.EvoToolsLocalization.Get(\"analytics.adapter_id.tooltip\", \"Optional override. Leave empty to use default adapter id from code.\")")]
        [SerializeField] private string adapterId;
        [SerializeField] private string appKey;

#if ODIN_INSPECTOR
        [Title("Extra Parameters")]
        [InfoBox("@_Project.Scripts.Editor.EvoTools.EvoToolsLocalization.Get(\"analytics.extra_params.info\", \"Add tooltip text to EvoTools localization table for parameter keys.\")", InfoMessageType.Info)]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true, ListElementLabelName = "DisplayName")]
#endif
        [SerializeField] private List<AnalyticsExtraParameter> extraParameters = new();

        public string AdapterId => adapterId;
        public string AppKey => appKey;
        public IReadOnlyList<AnalyticsExtraParameter> ExtraParameters => extraParameters;

        public string ResolveAdapterId(string defaultId)
        {
            return string.IsNullOrWhiteSpace(adapterId) ? defaultId : adapterId;
        }

    }

}
