using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using _Project.Scripts.Infrastructure.Services.Save;

namespace _Project.Scripts.Application.Loading
{
    public interface ISaveLoadStepHooks
    {
        void OnSaveLoaded(SaveEnvelope envelope, string playerName);
    }

    public sealed class SaveLoadStep : ILoadingStep
    {
        public string Message => "Loading save";
        public float Weight => 1f;
        public int Order => -10;

        private readonly IPlayerAuthService _playerAuthService;
        private readonly ISaveService _saveService;
        private readonly ISaveLoadStepHooks _hooks;

        public SaveLoadStep(
            IPlayerAuthService playerAuthService,
            ISaveService saveService,
            ISaveLoadStepHooks hooks = null)
        {
            _playerAuthService = playerAuthService;
            _saveService = saveService;
            _hooks = hooks;
        }

        public async UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
        {
            if (_playerAuthService != null)
            {
                await _playerAuthService.InitializeAsync(cancellationToken);
            }

            if (_saveService != null)
            {
                var envelope = await _saveService.LoadLatestValidAsync(cancellationToken);
                _hooks?.OnSaveLoaded(envelope, _playerAuthService?.PlayerName);
            }

            progress?.Report(1f);
        }
    }
}
