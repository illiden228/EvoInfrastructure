#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;

namespace Evo.Infrastructure.Core.Editor.Setup.Scaffold
{
    internal static class ScaffoldPlanBuilder
    {
        public static ScaffoldPlan Build(IEnumerable<(string Target, string Template)> scripts,
            IEnumerable<string> assets)
        {
            var items = new List<ScaffoldPlanItem>();
            foreach (var pair in scripts)
            {
                if (!File.Exists(pair.Template))
                {
                    items.Add(new ScaffoldPlanItem(pair.Target, ScaffoldChangeKind.Conflict, "Template is missing."));
                }
                else if (!File.Exists(pair.Target))
                {
                    items.Add(new ScaffoldPlanItem(pair.Target, ScaffoldChangeKind.Create, "Starter script is missing."));
                }
                else
                {
                    items.Add(new ScaffoldPlanItem(pair.Target, ScaffoldChangeKind.Preserve, "Existing project-owned script is never overwritten."));
                }
            }

            foreach (var path in assets)
            {
                var exists = File.Exists(path);
                items.Add(new ScaffoldPlanItem(
                    path,
                    exists ? ScaffoldChangeKind.Update : ScaffoldChangeKind.Create,
                    exists
                        ? "Existing asset will be validated and updated in place."
                        : "Starter asset is missing."));
            }

            return new ScaffoldPlan(items);
        }
    }
}
#endif
