using System;
using System.Collections.Generic;
using _Project.Scripts.Infrastructure.Services.Debug;

namespace _Project.Scripts.Infrastructure.Services.ScenePayload
{
    public sealed class ScenePayloadService : IScenePayloadService
    {
        private readonly Dictionary<string, object> _payloads = new(StringComparer.Ordinal);

        public void SetPayload(string sceneKey, object payload)
        {
            if (string.IsNullOrEmpty(sceneKey))
            {
                EvoDebug.LogWarning("SetPayload called with empty scene key.", nameof(ScenePayloadService));
                return;
            }

            _payloads[sceneKey] = payload;
        }

        public bool TryGetPayload<T>(string sceneKey, out T payload)
        {
            payload = default;
            if (string.IsNullOrEmpty(sceneKey))
            {
                EvoDebug.LogWarning("TryGetPayload called with empty scene key.", nameof(ScenePayloadService));
                return false;
            }

            if (_payloads.TryGetValue(sceneKey, out var value) && value is T typed)
            {
                payload = typed;
                return true;
            }

            return false;
        }

        public bool ConsumePayload<T>(string sceneKey, out T payload)
        {
            payload = default;
            if (string.IsNullOrEmpty(sceneKey))
            {
                EvoDebug.LogWarning("ConsumePayload called with empty scene key.", nameof(ScenePayloadService));
                return false;
            }

            if (_payloads.TryGetValue(sceneKey, out var value) && value is T typed)
            {
                _payloads.Remove(sceneKey);
                payload = typed;
                return true;
            }

            return false;
        }

        public void ClearPayload(string sceneKey)
        {
            if (string.IsNullOrEmpty(sceneKey))
            {
                EvoDebug.LogWarning("ClearPayload called with empty scene key.", nameof(ScenePayloadService));
                return;
            }

            _payloads.Remove(sceneKey);
        }
    }
}
