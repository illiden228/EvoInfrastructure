using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.TestTools;

namespace Evo.Infrastructure.Services.Audio.Tests
{
    public sealed class AudioServiceTests
    {
        private AudioService _service;

        [UnitySetUp]
        public IEnumerator EnterPlayModeBeforeTest()
        {
            yield return new EnterPlayMode();
        }

        [UnityTearDown]
        public IEnumerator ExitPlayModeAfterTest()
        {
            yield return new ExitPlayMode();
        }

        [SetUp]
        public void SetUp()
        {
            DestroyAudioRoot();
            _service = new AudioService(null);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            DestroyAudioRoot();
        }

        [Test]
        public void Dispose_CanBeCalledMoreThanOnce()
        {
            InitializeRuntime();

            Assert.DoesNotThrow(() => _service.Dispose());
            Assert.DoesNotThrow(() => _service.Dispose());
        }

        [Test]
        public void Dispose_ToleratesExternallyDestroyedRootAndSources()
        {
            InitializeRuntime();
            Object.DestroyImmediate(GameObject.Find("AudioServiceRoot"));

            Assert.DoesNotThrow(() => _service.Dispose());
        }

        [Test]
        public void Dispose_ToleratesExternallyDestroyedSource()
        {
            InitializeRuntime();
            var source = GameObject.Find("AudioServiceRoot").GetComponentInChildren<AudioSource>();
            Assert.That(source, Is.Not.Null);
            Object.DestroyImmediate(source.gameObject);

            Assert.DoesNotThrow(() => _service.Dispose());
        }

        [Test]
        public void PlayOneShot_AfterDispose_IsSafeNoOp()
        {
            var cue = ScriptableObject.CreateInstance<AudioCueKey>();
            try
            {
                _service.Dispose();

                Assert.DoesNotThrow(() => _service.PlayOneShot(cue, AudioLayers.Effects));
                Assert.That(GameObject.Find("AudioServiceRoot"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(cue);
            }
        }

        [Test]
        public void NullCue_BeforeDispose_DoesNotCreateRuntime()
        {
            Assert.DoesNotThrow(() => _service.PlayOneShot(null, AudioLayers.Effects));
            Assert.That(GameObject.Find("AudioServiceRoot"), Is.Null);
        }

        private void InitializeRuntime()
        {
            var method = typeof(AudioService).GetMethod(
                "EnsureRuntime",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(_service, null);
            Assert.That(GameObject.Find("AudioServiceRoot"), Is.Not.Null);
        }

        private static void DestroyAudioRoot()
        {
            var root = GameObject.Find("AudioServiceRoot");
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
