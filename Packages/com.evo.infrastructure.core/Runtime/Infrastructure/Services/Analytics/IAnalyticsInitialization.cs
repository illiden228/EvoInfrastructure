using System.Threading;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Infrastructure.Services.Analytics
{
    public interface IAnalyticsInitialization
    {
        UniTask WaitForInitializationAsync(CancellationToken cancellationToken);
    }
}
