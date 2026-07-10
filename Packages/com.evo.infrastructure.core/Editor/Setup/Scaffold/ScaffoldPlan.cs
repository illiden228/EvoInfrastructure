#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

namespace Evo.Infrastructure.Core.Editor.Setup.Scaffold
{
    internal enum ScaffoldChangeKind { Create, Update, Preserve, Conflict }

    internal readonly struct ScaffoldPlanItem
    {
        public ScaffoldPlanItem(string path, ScaffoldChangeKind kind, string reason)
        { Path = path; Kind = kind; Reason = reason; }
        public string Path { get; }
        public ScaffoldChangeKind Kind { get; }
        public string Reason { get; }
    }

    internal sealed class ScaffoldPlan
    {
        public ScaffoldPlan(IEnumerable<ScaffoldPlanItem> items) => Items = items.ToArray();
        public IReadOnlyList<ScaffoldPlanItem> Items { get; }
        public bool HasConflicts => Items.Any(item => item.Kind == ScaffoldChangeKind.Conflict);
        public bool HasChanges => Items.Any(item => item.Kind == ScaffoldChangeKind.Create || item.Kind == ScaffoldChangeKind.Update);
    }
}
#endif
