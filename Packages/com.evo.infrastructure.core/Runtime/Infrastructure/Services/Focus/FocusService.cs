using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace _Project.Scripts.Infrastructure.Services.Focus
{
    public sealed class FocusService : IFocusService
    {
        public FocusMode CurrentMode { get; private set; } = FocusMode.Ui;
        public bool IsGameplayInputEnabled => CurrentMode == FocusMode.Game;
        public bool IsUiLocked => _uiLockCount > 0;
        public event Action<FocusMode> FocusChanged;
        private readonly IDisposable _tick;
        private readonly List<RaycastResult> _raycastResults = new();
        private int _uiLockCount;

        public FocusService()
        {
            ApplyCursor(CurrentMode);
            _tick = Observable.EveryUpdate().Subscribe(_ => SyncWithCursor());
        }

        public void SetGameFocus(FocusChangeReason reason = FocusChangeReason.Unknown)
        {
            SetMode(FocusMode.Game);
        }

        public void SetUiFocus(FocusChangeReason reason = FocusChangeReason.Unknown)
        {
            SetMode(FocusMode.Ui);
        }

        public void Toggle(FocusChangeReason reason = FocusChangeReason.Unknown)
        {
            SetMode(CurrentMode == FocusMode.Game ? FocusMode.Ui : FocusMode.Game);
        }

        public void PushUiLock()
        {
            _uiLockCount++;
            if (_uiLockCount < 0)
            {
                _uiLockCount = 0;
            }
        }

        public void PopUiLock()
        {
            if (_uiLockCount > 0)
            {
                _uiLockCount--;
            }
        }

        private void SetMode(FocusMode mode)
        {
            if (CurrentMode == mode)
            {
                return;
            }

            CurrentMode = mode;
            ApplyCursor(CurrentMode);
            FocusChanged?.Invoke(CurrentMode);
        }

        private static void ApplyCursor(FocusMode mode)
        {
            if (mode == FocusMode.Game)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                return;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void SyncWithCursor()
        {
            if (!UnityEngine.Application.isFocused)
            {
                if (CurrentMode == FocusMode.Game)
                {
                    SetUiFocus(FocusChangeReason.External);
                }
                return;
            }

            if (CurrentMode == FocusMode.Game &&
                (Cursor.visible || Cursor.lockState != CursorLockMode.Locked))
            {
                ApplyCursor(FocusMode.Game);
            }

            if (CurrentMode == FocusMode.Ui &&
                Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame &&
                !IsPointerOverUi() &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                SetGameFocus(FocusChangeReason.External);
            }
        }

        private bool IsPointerOverUi()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            var position = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = position
            };

            _raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _raycastResults);
            return _raycastResults.Count > 0;
        }
    }
}
