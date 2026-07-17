using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evo.Infrastructure.DI
{
    /// <summary>
    /// Allows optional SDK assemblies to register strongly typed feature factories at Unity startup.
    /// </summary>
    public static class EvoOptionalFeatureRegistry
    {
        private static readonly object SyncRoot = new();
        private static readonly Dictionary<string, Action<EvoFeatureRegistry>> Factories =
            new(StringComparer.OrdinalIgnoreCase);

        public static void Register(string featureId, Action<EvoFeatureRegistry> registerFactory)
        {
            if (string.IsNullOrWhiteSpace(featureId))
            {
                throw new ArgumentException("Feature id is required.", nameof(featureId));
            }

            if (registerFactory == null)
            {
                throw new ArgumentNullException(nameof(registerFactory));
            }

            lock (SyncRoot)
            {
                if (Factories.TryGetValue(featureId.Trim(), out var existingFactory) &&
                    !Equals(existingFactory, registerFactory))
                {
                    Debug.LogWarning(
                        $"[EvoOptionalFeatureRegistry] Ignored conflicting factory registration for '{featureId}'. " +
                        "The first factory remains active.");
                    return;
                }

                Factories[featureId.Trim()] = registerFactory;
            }
        }

        public static bool TryRegister(EvoFeatureRegistry features, string featureId)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            Action<EvoFeatureRegistry> factory;
            lock (SyncRoot)
            {
                Factories.TryGetValue(featureId ?? string.Empty, out factory);
            }

            if (factory == null)
            {
                return false;
            }

            factory(features);
            return true;
        }
    }
}
