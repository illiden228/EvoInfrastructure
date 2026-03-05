using System;

namespace _Project.Scripts.Infrastructure.Services.Focus
{
    public interface IFocusService
    {
        FocusMode CurrentMode { get; }
        bool IsGameplayInputEnabled { get; }
        bool IsUiLocked { get; }
        event Action<FocusMode> FocusChanged;
        void PushUiLock();
        void PopUiLock();
        void SetGameFocus(FocusChangeReason reason = FocusChangeReason.Unknown);
        void SetUiFocus(FocusChangeReason reason = FocusChangeReason.Unknown);
        void Toggle(FocusChangeReason reason = FocusChangeReason.Unknown);
    }
}
