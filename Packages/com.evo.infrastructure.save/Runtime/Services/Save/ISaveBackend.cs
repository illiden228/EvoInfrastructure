using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Save
{
    public interface ISaveBackend
    {
        string BackendId { get; }
        int Priority { get; }
        bool IsAvailable { get; }
        UniTask<SaveEnvelope> LoadAsync(CancellationToken cancellationToken = default);
        UniTask<bool> SaveAsync(SaveEnvelope envelope, CancellationToken cancellationToken = default);
    }
}
