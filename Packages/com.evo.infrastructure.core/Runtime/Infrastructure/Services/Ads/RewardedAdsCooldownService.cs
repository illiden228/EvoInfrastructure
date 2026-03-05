using System;
using _Project.Scripts.Infrastructure.Services.Ads.Config;
using _Project.Scripts.Infrastructure.Services.Config;

namespace _Project.Scripts.Infrastructure.Services.Ads
{
    public interface IRewardedAdsCooldownState
    {
        long LastRewardedAdUnixMs { get; set; }
    }

    public sealed class RewardedAdsCooldownService
    {
        private readonly IRewardedAdsCooldownState _state;
        private readonly IConfigService _configService;
        private long _lastRewardedAdUnixMs;

        public RewardedAdsCooldownService(IConfigService configService, IRewardedAdsCooldownState state = null)
        {
            _configService = configService;
            _state = state;
        }

        public bool CanShow(out int remainingSeconds)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return CanShowAt(nowMs, out remainingSeconds);
        }

        public bool CanShowAt(long nowUnixMs, out int remainingSeconds)
        {
            remainingSeconds = 0;
            var cooldown = GetCooldownSeconds();
            if (cooldown <= 0)
            {
                return true;
            }

            var lastMs = _state != null
                ? _state.LastRewardedAdUnixMs
                : _lastRewardedAdUnixMs;
            if (lastMs <= 0)
            {
                return true;
            }

            var elapsed = (int)((nowUnixMs - lastMs) / 1000);
            remainingSeconds = Math.Max(0, cooldown - elapsed);
            return remainingSeconds <= 0;
        }

        public void RegisterShown(long nowUnixMs)
        {
            if (_state != null)
            {
                _state.LastRewardedAdUnixMs = nowUnixMs;
                return;
            }

            _lastRewardedAdUnixMs = nowUnixMs;
        }

        private int GetCooldownSeconds()
        {
            if (_configService == null)
            {
                return 0;
            }

            _configService.TryGet<AdsConfig>(out var config);
            return config != null ? config.RewardedCooldownSeconds : 0;
        }
    }
}
