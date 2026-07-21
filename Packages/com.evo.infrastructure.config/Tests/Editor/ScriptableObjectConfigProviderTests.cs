using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Services.Config.Tests
{
    public sealed class ScriptableObjectConfigProviderTests
    {
        [Test]
        public void TryGet_UsesAssetType_WhenSerializedTypeNameIsStale()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableConfigCatalog>();
            var config = ScriptableObject.CreateInstance<TestConfigAsset>();
            try
            {
                catalog.Upsert(config);
                var serializedCatalog = new SerializedObject(catalog);
                var entries = serializedCatalog.FindProperty("entries");
                entries.GetArrayElementAtIndex(0)
                    .FindPropertyRelative("TypeName")
                    .stringValue = "Legacy.Project.Config, Assembly-CSharp";
                serializedCatalog.ApplyModifiedPropertiesWithoutUndo();

                var provider = new ScriptableObjectConfigProvider(
                    new List<ScriptableConfigCatalog> { catalog });

                Assert.That(provider.TryGet(typeof(TestConfigAsset), out var resolved), Is.True);
                Assert.That(resolved, Is.SameAs(config));
            }
            finally
            {
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(catalog);
            }
        }

        private sealed class TestConfigAsset : ScriptableObject
        {
        }
    }
}
