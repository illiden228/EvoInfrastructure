namespace Evo.Infrastructure.Services.Audio
{
    public interface IAudioService
    {
        void PlayBackground(AudioCueKey cueKey, bool restartIfSame = false);
        void StopBackground();
        void PlayLoop(AudioCueKey cueKey, AudioLayerKey layer, bool restartIfSame = false);
        void StopLoop(AudioLayerKey layer);
        void StopAllLoops();
        void PlayEffect(AudioCueKey cueKey);
        void PlayUiEffect(AudioCueKey cueKey);
        void PlayOneShot(AudioCueKey cueKey, AudioLayerKey layer);
        void SetMasterVolume(float volume);
        void SetLayerVolume(AudioLayer layer, float volume);
        void SetLayerVolume(AudioLayerKey layer, float volume);
    }
}
