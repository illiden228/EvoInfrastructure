using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evo.Infrastructure.Services.Save
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        public int schemaVersion = 1;
        public long updatedAtUnixMs;
        public List<SavePayloadData> payloads = new();

        public static string GetDefaultPayloadKey<T>() where T : class
        {
            return typeof(T).FullName ?? typeof(T).Name;
        }

        public bool TryGetPayload<T>(out T payload) where T : class
        {
            return TryGetPayload(GetDefaultPayloadKey<T>(), out payload);
        }

        public bool TryGetPayload<T>(string key, out T payload) where T : class
        {
            payload = default;
            if (!TryGetPayloadJson(key, out var json))
            {
                return false;
            }

            try
            {
                payload = JsonUtility.FromJson<T>(json);
                return payload != null;
            }
            catch
            {
                payload = default;
                return false;
            }
        }

        public T GetPayload<T>(string key, T fallback = default) where T : class
        {
            return TryGetPayload<T>(key, out var payload) ? payload : fallback;
        }

        public T GetPayload<T>(T fallback = default) where T : class
        {
            return GetPayload(GetDefaultPayloadKey<T>(), fallback);
        }

        public bool TryGetPayloadJson(string key, out string json)
        {
            var data = GetPayloadData(key);
            if (data == null || string.IsNullOrWhiteSpace(data.json))
            {
                json = null;
                return false;
            }

            json = data.json;
            return true;
        }

        public int GetPayloadVersion(string key)
        {
            return GetPayloadData(key)?.version ?? 0;
        }

        public void SetPayload<T>(T payload, int version = 1) where T : class
        {
            SetPayload(GetDefaultPayloadKey<T>(), payload, version);
        }

        public void SetPayload<T>(string key, T payload, int version = 1) where T : class
        {
            if (payload == null)
            {
                RemovePayload(key);
                return;
            }

            SetPayloadJson(key, JsonUtility.ToJson(payload), version);
        }

        public void SetPayloadJson(string key, string json, int version = 1)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                RemovePayload(key);
                return;
            }

            key = NormalizePayloadKey(key);
            var data = GetOrCreatePayloadData(key);
            data.version = version > 0 ? version : 1;
            data.json = json;
        }

        public bool RemovePayload<T>() where T : class
        {
            return RemovePayload(GetDefaultPayloadKey<T>());
        }

        public bool RemovePayload(string key)
        {
            key = NormalizePayloadKey(key);
            if (payloads == null)
            {
                return false;
            }

            for (var i = payloads.Count - 1; i >= 0; i--)
            {
                if (string.Equals(payloads[i]?.key, key, StringComparison.Ordinal))
                {
                    payloads.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private SavePayloadData GetPayloadData(string key)
        {
            key = NormalizePayloadKey(key);
            if (payloads == null)
            {
                return null;
            }

            for (var i = 0; i < payloads.Count; i++)
            {
                var data = payloads[i];
                if (data != null && string.Equals(data.key, key, StringComparison.Ordinal))
                {
                    return data;
                }
            }

            return null;
        }

        private SavePayloadData GetOrCreatePayloadData(string key)
        {
            if (payloads == null)
            {
                payloads = new List<SavePayloadData>();
            }

            var data = GetPayloadData(key);
            if (data != null)
            {
                return data;
            }

            data = new SavePayloadData { key = key };
            payloads.Add(data);
            return data;
        }

        private static string NormalizePayloadKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? "default" : key;
        }
    }
}
