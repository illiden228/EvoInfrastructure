using System;
using System.Threading;
using _Project.Scripts.Infrastructure.Services.PlatformInfo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Application.Loading
{
    public sealed class TargetFrameRateStep : ILoadingStep
    {
        public string Message => "Setting target frame rate";
        public float Weight => 1f;
        public int Order => -20;

        private readonly IPlatformInfoService _platformInfoService;
        
        public TargetFrameRateStep(IPlatformInfoService  platformInfoService)
        {
            _platformInfoService = platformInfoService;
        }

        public UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
        {
            if (_platformInfoService.IsMobileWeb)
            {
                QualitySettings.vSyncCount = 0;
                UnityEngine.Application.targetFrameRate = 60;
            }
            
            progress?.Report(1f);
            return UniTask.CompletedTask;
        }
    }
}
