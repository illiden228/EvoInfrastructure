using System;
using System.Collections.Generic;
using System.Threading;
using _Project.Scripts.Infrastructure.Services.Debug;
using _Project.Scripts.Infrastructure.Services.ResourceLoader;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.Audio
{
    public sealed class AudioService : IAudioService, IDisposable
    {
        private const string ROOT_NAME = "AudioServiceRoot";
        private const string AUDIO_SERVICE_SOURCE = "AudioService";

        private readonly IResourceLoaderService _resourceLoader;
        private readonly Dictionary<AudioCueKey, AudioClip> _clipCache = new();
        private readonly Dictionary<AudioLayer, float> _layerVolumes = new()
        {
            { AudioLayer.Background, 1f },
            { AudioLayer.Effects, 1f },
            { AudioLayer.UiEffects, 1f }
        };
        private readonly HashSet<string> _loadedClipKeys = new();

        private GameObject _root;
        private AudioSource _backgroundSource;
        private AudioSource _effectsSource;
        private AudioSource _uiEffectsSource;

        private float _masterVolume = 1f;
        private int _backgroundRequestVersion;
        private AudioCueKey _currentBackgroundCue;
        private bool _disposed;

        public AudioService(IResourceLoaderService resourceLoader)
        {
            _resourceLoader = resourceLoader;
        }

        public void PlayBackground(AudioCueKey cueKey, bool restartIfSame = false)
        {
            if (_disposed || cueKey == null)
            {
                EvoDebug.LogWarning("PlayBackground skipped: disposed service or null cue.", AUDIO_SERVICE_SOURCE);
                return;
            }

            EnsureRuntime();
            var requestVersion = ++_backgroundRequestVersion;
            PlayBackgroundInternal(cueKey, restartIfSame, requestVersion).Forget();
        }

        public void StopBackground()
        {
            if (_disposed)
            {
                EvoDebug.LogWarning("StopBackground skipped: disposed service.", AUDIO_SERVICE_SOURCE);
                return;
            }

            EnsureRuntime();
            _backgroundRequestVersion++;
            _currentBackgroundCue = null;
            _backgroundSource.Stop();
            _backgroundSource.clip = null;
        }

        public void PlayEffect(AudioCueKey cueKey)
        {
            if (_disposed || cueKey == null)
            {
                EvoDebug.LogWarning("PlayEffect skipped: disposed service or null cue.", AUDIO_SERVICE_SOURCE);
                return;
            }

            EnsureRuntime();
            PlayOneShotInternal(cueKey, AudioLayer.Effects).Forget();
        }

        public void PlayUiEffect(AudioCueKey cueKey)
        {
            if (_disposed || cueKey == null)
            {
                EvoDebug.LogWarning("PlayUiEffect skipped: disposed service or null cue.", AUDIO_SERVICE_SOURCE);
                return;
            }

            EnsureRuntime();
            PlayOneShotInternal(cueKey, AudioLayer.UiEffects).Forget();
        }

        public void SetMasterVolume(float volume)
        {
            if (_disposed)
            {
                return;
            }

            _masterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetLayerVolume(AudioLayer layer, float volume)
        {
            if (_disposed)
            {
                return;
            }

            _layerVolumes[layer] = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _backgroundRequestVersion++;

            if (_backgroundSource != null)
            {
                _backgroundSource.Stop();
            }

            foreach (var loadedKey in _loadedClipKeys)
            {
                _resourceLoader.Release<AudioClip>(loadedKey);
            }

            _loadedClipKeys.Clear();
            _clipCache.Clear();

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }

            EvoDebug.Log("Disposed runtime and released loaded clips.", AUDIO_SERVICE_SOURCE);
        }

        private async UniTaskVoid PlayBackgroundInternal(AudioCueKey cueKey, bool restartIfSame, int requestVersion)
        {
            var clip = await ResolveClipAsync(cueKey);
            if (_disposed || requestVersion != _backgroundRequestVersion || clip == null)
            {
                if (clip == null)
                {
                    EvoDebug.LogWarning($"Background clip resolve failed for cue '{cueKey?.name}'.", AUDIO_SERVICE_SOURCE);
                }
                return;
            }

            var isSameCue = _currentBackgroundCue == cueKey;
            if (isSameCue && _backgroundSource.isPlaying && !restartIfSame)
            {
                return;
            }

            _currentBackgroundCue = cueKey;
            _backgroundSource.loop = true;
            _backgroundSource.clip = clip;
            _backgroundSource.Play();
            EvoDebug.Log($"Background started: cue '{cueKey.name}', clip '{clip.name}'.", AUDIO_SERVICE_SOURCE);
        }

        private async UniTaskVoid PlayOneShotInternal(AudioCueKey cueKey, AudioLayer layer)
        {
            var clip = await ResolveClipAsync(cueKey);
            if (_disposed || clip == null)
            {
                if (clip == null)
                {
                    EvoDebug.LogWarning($"OneShot clip resolve failed for cue '{cueKey?.name}', layer '{layer}'.", AUDIO_SERVICE_SOURCE);
                }
                return;
            }

            var source = GetSource(layer);
            source.PlayOneShot(clip);
            EvoDebug.Log($"OneShot played: cue '{cueKey.name}', clip '{clip.name}', layer '{layer}'.", AUDIO_SERVICE_SOURCE);
        }

        private async UniTask<AudioClip> ResolveClipAsync(AudioCueKey cueKey)
        {
            if (cueKey == null || !cueKey.HasClip || cueKey.Clip == null)
            {
                EvoDebug.LogWarning($"ResolveClipAsync skipped: invalid cue '{cueKey?.name}'.", AUDIO_SERVICE_SOURCE);
                return null;
            }

            if (_clipCache.TryGetValue(cueKey, out var cachedClip) && cachedClip != null)
            {
                return cachedClip;
            }

            var clip = await _resourceLoader.GetOrLoadAsync<AudioClip>(cueKey.Clip, CancellationToken.None);
            if (clip == null)
            {
                EvoDebug.LogError($"Failed to load clip from cue '{cueKey.name}'.", AUDIO_SERVICE_SOURCE);
                return null;
            }

            _clipCache[cueKey] = clip;
            var cacheReleaseKey = !string.IsNullOrEmpty(cueKey.Clip.AssetGUID)
                ? cueKey.Clip.AssetGUID
                : cueKey.Clip.RuntimeKey?.ToString();
            if (!string.IsNullOrEmpty(cacheReleaseKey))
            {
                _loadedClipKeys.Add(cacheReleaseKey);
            }

            return clip;
        }

        private void EnsureRuntime()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject(ROOT_NAME);
            UnityEngine.Object.DontDestroyOnLoad(_root);

            _backgroundSource = CreateSource("BackgroundSource", loop: true);
            _effectsSource = CreateSource("EffectsSource", loop: false);
            _uiEffectsSource = CreateSource("UiEffectsSource", loop: false);

            ApplyVolumes();
            EvoDebug.Log("Runtime initialized: root and audio sources created.", AUDIO_SERVICE_SOURCE);
        }

        private AudioSource CreateSource(string name, bool loop)
        {
            var sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(_root.transform, false);

            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private AudioSource GetSource(AudioLayer layer)
        {
            return layer switch
            {
                AudioLayer.Background => _backgroundSource,
                AudioLayer.UiEffects => _uiEffectsSource,
                _ => _effectsSource
            };
        }

        private void ApplyVolumes()
        {
            if (_root == null)
            {
                return;
            }

            _backgroundSource.volume = GetLayerOutputVolume(AudioLayer.Background);
            _effectsSource.volume = GetLayerOutputVolume(AudioLayer.Effects);
            _uiEffectsSource.volume = GetLayerOutputVolume(AudioLayer.UiEffects);
        }

        private float GetLayerOutputVolume(AudioLayer layer)
        {
            if (!_layerVolumes.TryGetValue(layer, out var layerVolume))
            {
                layerVolume = 1f;
            }

            return Mathf.Clamp01(_masterVolume * layerVolume);
        }
    }
}
