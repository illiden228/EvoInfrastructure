using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Analytics
{
    public interface IAnalyticsInitialization
    {
        UniTask WaitForInitializationAsync(CancellationToken cancellationToken);
    }
}
