using R3;

namespace _Project.Scripts.Application.Loading
{
    public sealed class LoadingScreenModel
    {
        public ReactiveProperty<float> Progress { get; } = new(0f);
        public ReactiveProperty<string> Message { get; } = new(string.Empty);

        public void Reset()
        {
            Progress.Value = 0f;
            Message.Value = string.Empty;
        }
    }
}
