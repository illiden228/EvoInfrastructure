using System;
using System.Collections.Generic;
using System.Text;

namespace Evo.Infrastructure.Runtime.Loading
{
    internal static class LoadingStepOrdering
    {
        public static List<ILoadingStep> Prepare(
            IReadOnlyList<ILoadingStep> steps,
            LoadingStepOrderMode mode)
        {
            var result = new List<ILoadingStep>(steps?.Count ?? 0);
            if (steps == null)
            {
                return result;
            }

            for (var i = 0; i < steps.Count; i++)
            {
                result.Add(steps[i]);
            }

            if (mode == LoadingStepOrderMode.OrderProperty)
            {
                result.Sort(CompareByOrder);
            }

            return result;
        }

        public static string FormatPlan(
            IReadOnlyList<ILoadingStep> steps,
            LoadingStepOrderMode mode,
            LoadingExecutionOptions options,
            string planName)
        {
            var builder = new StringBuilder();
            builder.Append('[').Append(planName).Append("] mode=").Append(mode);
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                builder.AppendLine();
                builder.Append(i + 1).Append(". ");
                if (step == null)
                {
                    builder.Append("<null>");
                    continue;
                }

                builder.Append(step.GetType().Name)
                    .Append(" order=").Append(step.Order)
                    .Append(" weight=").Append(step.Weight.ToString("0.###"))
                    .Append(" timeout=")
                    .Append(LoadingStepExecution.ResolveTimeoutSeconds(step, options).ToString("0.###"));
            }

            return builder.ToString();
        }

        private static int CompareByOrder(ILoadingStep left, ILoadingStep right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return left.Order.CompareTo(right.Order);
        }
    }
}
