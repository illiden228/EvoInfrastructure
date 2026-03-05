namespace _Project.Scripts.Infrastructure.Services.Audio
{
    public interface IAudioService
    {
        void PlayBackground(AudioCueKey cueKey, bool restartIfSame = false);
        void StopBackground();
        void PlayEffect(AudioCueKey cueKey);
        void PlayUiEffect(AudioCueKey cueKey);
        void SetMasterVolume(float volume);
        void SetLayerVolume(AudioLayer layer, float volume);
    }
}
