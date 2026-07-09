using Cysharp.Threading.Tasks;
#if YandexGamesPlatform_yg
using YG;
#endif

namespace Evo.Infrastructure.Services.Save
{
    public sealed class YandexPlayerAuthService : IPlayerAuthService
    {
        public bool IsAuthorized { get; private set; }
        public string PlayerName { get; private set; } = string.Empty;

        public UniTask InitializeAsync(System.Threading.CancellationToken cancellationToken = default)
        {
#if YandexGamesPlatform_yg && Authorization_yg
            if (!YG2.isSDKEnabled)
            {
                IsAuthorized = false;
                PlayerName = string.Empty;
                return UniTask.CompletedTask;
            }

            YG2.GetAuth();
            IsAuthorized = YG2.player.auth;
#else
            IsAuthorized = false;
#endif
            PlayerName = string.Empty;
            return UniTask.CompletedTask;
        }

        public UniTask RequestAuthorizationAsync(System.Threading.CancellationToken cancellationToken = default)
        {
#if YandexGamesPlatform_yg && Authorization_yg
            YG2.OpenAuthDialog();
            YG2.GetAuth();
            IsAuthorized = YG2.player.auth;
#else
            IsAuthorized = false;
#endif
            return UniTask.CompletedTask;
        }
    }
}
