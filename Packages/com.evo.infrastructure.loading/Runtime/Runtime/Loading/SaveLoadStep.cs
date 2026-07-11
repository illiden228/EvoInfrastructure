using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Save;

namespace Evo.Infrastructure.Runtime.Loading
{
    public sealed class SaveLoadStep : ILoadingStep
    {
        public string Message => "Loading save";
        public float Weight => 1f;
        public int Order => -10;

        private readonly ISaveService _saveService;
        private readonly ISaveLoadStepHooks _hooks;

        public SaveLoadStep(
            ISaveService saveService,
            ISaveLoadStepHooks hooks = null)
        {
            _saveService = saveService;
            _hooks = hooks;
        }

        public async UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
        {
            if (_saveService != null)
            {
                var envelope = await _saveService.LoadLatestValidAsync(cancellationToken);
                _hooks?.OnSaveLoaded(envelope);
            }

            progress?.Report(1f);
        }
    }
}
