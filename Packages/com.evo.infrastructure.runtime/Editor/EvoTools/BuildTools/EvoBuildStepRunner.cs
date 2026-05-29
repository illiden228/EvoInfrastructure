using System.Collections.Generic;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    internal static class EvoBuildStepRunner
    {
        public static void Validate(EvoBuildContext context, EvoBuildDryRunReport report)
        {
            var steps = CollectAllEnabled(context?.Profile);
            for (var i = 0; i < steps.Count; i++)
            {
                steps[i].Validate(context, report);
            }
        }

        public static bool Execute(EvoBuildContext context, EvoBuildStepPhase phase, EvoBuildApplyResult result)
        {
            var steps = Collect(context?.Profile, phase);
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (!step.Execute(context, result))
                {
                    result.AddError($"Build step failed: {step.name} ({phase}).");
                    return false;
                }
            }

            return true;
        }

        public static void Cleanup(EvoBuildContext context, EvoBuildApplyResult result)
        {
            var steps = CollectCleanup(context?.Profile);
            for (var i = steps.Count - 1; i >= 0; i--)
            {
                steps[i].Cleanup(context, result);
            }
        }

        private static List<EvoBuildStepAsset> Collect(PlatformBuildProfile profile, EvoBuildStepPhase phase)
        {
            var result = new List<EvoBuildStepAsset>();
            var steps = profile?.Steps;
            if (steps == null)
            {
                return result;
            }

            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step != null && step.Enabled && step.Phase == phase)
                {
                    result.Add(step);
                }
            }

            result.Sort((left, right) =>
            {
                var order = left.Order.CompareTo(right.Order);
                return order != 0 ? order : string.CompareOrdinal(left.name, right.name);
            });
            return result;
        }

        private static List<EvoBuildStepAsset> CollectAllEnabled(PlatformBuildProfile profile)
        {
            var result = new List<EvoBuildStepAsset>();
            var steps = profile?.Steps;
            if (steps == null)
            {
                return result;
            }

            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step != null && step.Enabled)
                {
                    result.Add(step);
                }
            }

            result.Sort((left, right) =>
            {
                var phase = left.Phase.CompareTo(right.Phase);
                if (phase != 0)
                {
                    return phase;
                }

                var order = left.Order.CompareTo(right.Order);
                return order != 0 ? order : string.CompareOrdinal(left.name, right.name);
            });
            return result;
        }

        private static List<IEvoBuildCleanupStep> CollectCleanup(PlatformBuildProfile profile)
        {
            var result = new List<IEvoBuildCleanupStep>();
            var steps = profile?.Steps;
            if (steps == null)
            {
                return result;
            }

            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step != null && step.Enabled && step is IEvoBuildCleanupStep cleanupStep)
                {
                    result.Add(cleanupStep);
                }
            }

            return result;
        }
    }
}
