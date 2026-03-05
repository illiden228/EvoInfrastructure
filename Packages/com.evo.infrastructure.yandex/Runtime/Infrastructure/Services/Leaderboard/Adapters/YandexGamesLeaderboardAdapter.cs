using _Project.Scripts.Infrastructure.Services.PlatformInfo;
#if YandexGamesPlatform_yg
using YG;
#endif

namespace _Project.Scripts.Infrastructure.Services.Leaderboard.Adapters
{
    public sealed class YandexGamesLeaderboardAdapter : ILeaderboardAdapter
    {
        private const string ADAPTER_ID = "yandex";
        private readonly IPlatformInfoService _platformInfoService;

        public YandexGamesLeaderboardAdapter(IPlatformInfoService platformInfoService = null)
        {
            _platformInfoService = platformInfoService;
        }

        public string AdapterId => ADAPTER_ID;
        public int Priority => 10;
        public bool IsInitialized
        {
            get
            {
#if YandexGamesPlatform_yg
                return YG2.isSDKEnabled;
#else
                return false;
#endif
            }
        }

        public bool IsAvailable => _platformInfoService == null || _platformInfoService.IsWeb;

        public bool Supports(bool isTimeSeconds)
        {
            return true;
        }

        public void Submit(in LeaderboardSubmitRequest request)
        {
#if YandexGamesPlatform_yg
            if (!IsInitialized)
            {
                return;
            }

            if (request.IsTimeSeconds)
            {
                YG2.SetLBTimeConvert(request.LeaderboardKey, request.TimeSeconds, request.ExtraData);
            }
            else
            {
                YG2.SetLeaderboard(request.LeaderboardKey, request.Score, request.ExtraData);
            }
#endif
        }
    }
}
