using System;
using System.Collections.Generic;
using System.Threading;
using Evo.Infrastructure.Services.Debug;
using Evo.Infrastructure.Services.ResourceLoader;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Evo.Infrastructure.Services.Audio
{
    public sealed class AudioService : IAudioService, IDisposable
    {
        private const string ROOT_NAME = "AudioServiceRoot";
        private const string AUDIO_SERVICE_SOURCE = "AudioService";

        private readonly IResourceLoaderService _resourceLoader;
        private readonly Dictionary<AudioCueKey, AudioClip> _clipCache = new();
        private readonly Dictionary<AudioLayerKey, float> _layerVolumes = new()
        {
            { AudioLayers.Background, 1f },
            { AudioLayers.Effects, 1f },
            { AudioLayers.UiEffects, 1f }
        };
        private readonly Dictionary<AudioLayerKey, LoopPlayback> _loopPlaybacks = new();
        private readonly Dictionary<AudioLayerKey, AudioSource> _oneShotSources = new();
        private readonly HashSet<string> _loadedClipKeys = new();

        private GameObject _root;

        private float _masterVolume = 1f;
        private bool _disposed;

        public AudioService(IResourceLoaderService resourceLoader)
        {
            _resourceLoader = resourceLoader;
        }

        public void PlayBackground(AudioCueKey cueKey, bool restartIfSame = false)
        {
            PlayLoop(cueKey, AudioLayers.Background, restartIfSame);
        }

        public void StopBackground()
        {
            StopLoop(AudioLayers.Background);
        }

        public void PlayLoop(AudioCueKey cueKey, AudioLayerKey layer, bool restartIfSame = false)
        {
            var safeLayer = GetSafeLayer(layer);
            if (_disposed)
            {
                return;
            }

            if (cueKey == null)
            {
                EvoDebug.LogWarning($"PlayLoop skipped: null cue, layer '{safeLayer}'.", AUDIO_SERVICE_SOURCE);
                return;
            }

            EnsureRuntime();
            var playback = GetOrCreateLoopPlayback(safeLayer);
            var requestVersion = ++playback.RequestVersion;
            PlayLoopInternal(cueKey, safeLayer, playback, restartIfSame, requestVersion).Forget();
        }

        public void StopLoop(AudioLayerKey layer)
        {
            var safeLayer = GetSafeLayer(layer);
            if (_disposed)
            {
                return;
            }

            EnsureRuntime();
            if (!_loopPlaybacks.TryGetValue(safeLayer, out var playback))
            {
                return;
            }

            playback.RequestVersion++;
            playback.CurrentCue = null;
            if (playback.Source != null)
            {
                playback.Source.Stop();
                playback.Source.clip = null;
            }
        }

        public void StopAllLoops()
        {
            if (_disposed)
            {
                return;
            }

            EnsureRuntime();
            foreach (var playback in _loopPlaybacks.Values)
            {
                playback.RequestVersion++;
                playback.CurrentCue = null;
                if (playback.Source != null)
                {
                    playback.Source.Stop();
                    playback.Source.clip = null;
                }
            }
        }

        public void PlayEffect(AudioCueKey cueKey)
        {
            PlayOneShot(cueKey, AudioLayers.Effects);
        }

        public void PlayUiEffect(AudioCueKey cueKey)
        {
            PlayOneShot(cueKey, AudioLayers.UiEffects);
        }

        public void PlayOneShot(AudioCueKey cueKey, AudioLayerKey layer)
        {
            var safeLayer = GetSafeLayer(layer);
            if (_disposed)
            {
                return;
            }

            if (cueKey == null)
            {
                EvoDebug.LogWarning($"PlayOneShot skipped: null cue, layer '{safeLayer}'.", AUDIO_SERVICE_SOURCE);
                return;
            }

            EnsureRuntime();
            PlayOneShotInternal(cueKey, safeLayer).Forget();
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
            SetLayerVolume(ToLayerKey(layer), volume);
        }

        public void SetLayerVolume(AudioLayerKey layer, float volume)
        {
            if (_disposed)
            {
                return;
            }

            _layerVolumes[GetSafeLayer(layer)] = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var playback in _loopPlaybacks.Values)
            {
                playback.RequestVersion++;
                if (playback.Source != null)
                {
                    playback.Source.Stop();
                }
            }

            foreach (var source in _oneShotSources.Values)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }

            foreach (var loadedKey in _loadedClipKeys)
            {
                _resourceLoader.Release<AudioClip>(loadedKey);
            }

            _loadedClipKeys.Clear();
            _clipCache.Clear();
            _loopPlaybacks.Clear();
            _oneShotSources.Clear();

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }

            EvoDebug.Log("Disposed runtime and released loaded clips.", AUDIO_SERVICE_SOURCE);
        }

        private async UniTaskVoid PlayLoopInternal(
            AudioCueKey cueKey,
            AudioLayerKey layer,
            LoopPlayback playback,
            bool restartIfSame,
            int requestVersion)
        {
            var clip = await ResolveClipAsync(cueKey);
            if (_disposed)
            {
                return;
            }

            if (requestVersion != playback.RequestVersion || playback.Source == null || clip == null)
            {
                if (clip == null)
                {
                    EvoDebug.LogWarning($"Loop clip resolve failed for cue '{cueKey?.name}', layer '{layer}'.", AUDIO_SERVICE_SOURCE);
                }
                return;
            }

            var isSameCue = playback.CurrentCue == cueKey;
            if (isSameCue && playback.Source.isPlaying && !restartIfSame)
            {
                return;
            }

            playback.CurrentCue = cueKey;
            playback.Source.loop = true;
            playback.Source.clip = clip;
            playback.Source.Play();
            EvoDebug.Log($"Loop started: cue '{cueKey.name}', clip '{clip.name}', layer '{layer}'.", AUDIO_SERVICE_SOURCE);
        }

        private async UniTaskVoid PlayOneShotInternal(AudioCueKey cueKey, AudioLayerKey layer)
        {
            var clip = await ResolveClipAsync(cueKey);
            if (_disposed)
            {
                return;
            }

            if (clip == null)
            {
                EvoDebug.LogWarning($"OneShot clip resolve failed for cue '{cueKey?.name}', layer '{layer}'.", AUDIO_SERVICE_SOURCE);
                return;
            }

            EnsureRuntime();
            var source = GetOrCreateOneShotSource(layer);
            if (_disposed || source == null)
            {
                return;
            }

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

            if (_disposed)
            {
                var releaseKey = GetClipReleaseKey(cueKey);
                if (!string.IsNullOrEmpty(releaseKey))
                {
                    _resourceLoader.Release<AudioClip>(releaseKey);
                }
                return null;
            }

            _clipCache[cueKey] = clip;
            var cacheReleaseKey = GetClipReleaseKey(cueKey);
            if (!string.IsNullOrEmpty(cacheReleaseKey))
            {
                _loadedClipKeys.Add(cacheReleaseKey);
            }

            return clip;
        }

        private void EnsureRuntime()
        {
            if (_disposed || _root != null)
            {
                return;
            }

            _root = new GameObject(ROOT_NAME);
            UnityEngine.Object.DontDestroyOnLoad(_root);

            GetOrCreateLoopPlayback(AudioLayers.Background);
            GetOrCreateOneShotSource(AudioLayers.Effects);
            GetOrCreateOneShotSource(AudioLayers.UiEffects);

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

        private LoopPlayback GetOrCreateLoopPlayback(AudioLayerKey layer)
        {
            layer = GetSafeLayer(layer);
            if (_loopPlaybacks.TryGetValue(layer, out var playback) && playback.Source != null)
            {
                return playback;
            }

            var source = CreateSource($"LoopSource_{layer}", loop: true);
            source.volume = GetLayerOutputVolume(layer);
            playback = new LoopPlayback(source);
            _loopPlaybacks[layer] = playback;
            return playback;
        }

        private AudioSource GetOrCreateOneShotSource(AudioLayerKey layer)
        {
            layer = GetSafeLayer(layer);
            if (_oneShotSources.TryGetValue(layer, out var source) && source != null)
            {
                return source;
            }

            source = CreateSource($"OneShotSource_{layer}", loop: false);
            source.volume = GetLayerOutputVolume(layer);
            _oneShotSources[layer] = source;
            return source;
        }

        private void ApplyVolumes()
        {
            if (_root == null)
            {
                return;
            }

            foreach (var pair in _loopPlaybacks)
            {
                if (pair.Value.Source != null)
                {
                    pair.Value.Source.volume = GetLayerOutputVolume(pair.Key);
                }
            }

            foreach (var pair in _oneShotSources)
            {
                if (pair.Value != null)
                {
                    pair.Value.volume = GetLayerOutputVolume(pair.Key);
                }
            }
        }

        private static string GetClipReleaseKey(AudioCueKey cueKey)
        {
            return !string.IsNullOrEmpty(cueKey?.Clip?.AssetGUID)
                ? cueKey.Clip.AssetGUID
                : cueKey?.Clip?.RuntimeKey?.ToString();
        }

        private float GetLayerOutputVolume(AudioLayerKey layer)
        {
            if (!_layerVolumes.TryGetValue(GetSafeLayer(layer), out var layerVolume))
            {
                layerVolume = 1f;
            }

            return Mathf.Clamp01(_masterVolume * layerVolume);
        }

        private static AudioLayerKey GetSafeLayer(AudioLayerKey layer)
        {
            return string.IsNullOrWhiteSpace(layer.Id) ? AudioLayers.Default : layer;
        }

        private static AudioLayerKey ToLayerKey(AudioLayer layer)
        {
            return layer switch
            {
                AudioLayer.Background => AudioLayers.Background,
                AudioLayer.UiEffects => AudioLayers.UiEffects,
                _ => AudioLayers.Effects
            };
        }

        private sealed class LoopPlayback
        {
            public LoopPlayback(AudioSource source)
            {
                Source = source;
            }

            public AudioSource Source { get; }
            public AudioCueKey CurrentCue { get; set; }
            public int RequestVersion { get; set; }
        }
    }
}
