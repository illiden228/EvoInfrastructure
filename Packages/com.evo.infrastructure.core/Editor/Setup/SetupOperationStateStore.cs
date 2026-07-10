#if UNITY_EDITOR
using UnityEditor;

namespace Evo.Infrastructure.Core.Editor.Setup
{
    [FilePath("Library/EvoInfrastructure/SetupOperation.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class SetupOperationStateStore : ScriptableSingleton<SetupOperationStateStore>
    {
        public SetupOperationState State = SetupOperationState.Idle;
        public string Status = string.Empty;
        public double StartedAt;

        public void Set(SetupOperationState state, string status)
        {
            State = state;
            Status = status ?? string.Empty;
            if (state == SetupOperationState.Analyze || StartedAt <= 0d)
                StartedAt = EditorApplication.timeSinceStartup;
            Save(true);
        }

        public void Reset()
        {
            State = SetupOperationState.Idle;
            Status = string.Empty;
            StartedAt = 0d;
            Save(true);
        }
    }
}
#endif
