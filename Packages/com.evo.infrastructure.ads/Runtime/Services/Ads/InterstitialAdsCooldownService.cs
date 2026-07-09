using System;
using Evo.Infrastructure.Services.Ads.Config;
using Evo.Infrastructure.Services.Config;

namespace Evo.Infrastructure.Services.Ads
{
    public sealed class InterstitialAdsCooldownService
    {
        private const int DefaultCooldownSeconds = 300;
        private readonly IConfigService _configService;
        private readonly IInterstitialAdsCooldownState _state;
        private long _cooldownUntilUnixMs;
        private bool _isWarm;

        public InterstitialAdsCooldownService(
            IConfigService configService = null,
            IInterstitialAdsCooldownState state = null)
        {
            _configService = configService;
            _state = state;
        }

        public bool IsWarm => _state != null ? _state.InterstitialWarm : _isWarm;

        public bool CanShow(out int remainingSeconds)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return CanShowAt(nowMs, out remainingSeconds);
        }

        public bool CanShowAt(long nowUnixMs, out int remainingSeconds)
        {
            var cooldownUntil = _state != null
                ? _state.InterstitialCooldownUntilUnixMs
                : _cooldownUntilUnixMs;
            remainingSeconds = cooldownUntil > nowUnixMs
                ? (int)Math.Ceiling((cooldownUntil - nowUnixMs) / 1000d)
                : 0;
            return remainingSeconds <= 0;
        }

        public void RegisterShown(long nowUnixMs)
        {
            var cooldownUntil = nowUnixMs + GetCooldownSeconds() * 1000L;
            if (_state != null)
            {
                _state.InterstitialCooldownUntilUnixMs = cooldownUntil;
                _state.InterstitialWarm = true;
                return;
            }

            _cooldownUntilUnixMs = cooldownUntil;
            _isWarm = true;
        }

        private int GetCooldownSeconds()
        {
            if (_configService != null && _configService.TryGet<AdsConfig>(out var config) && config != null)
            {
                return Math.Max(0, config.InterstitialCooldownSeconds);
            }

            return DefaultCooldownSeconds;
        }
    }
}
