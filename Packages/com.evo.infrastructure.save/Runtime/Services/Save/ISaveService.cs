using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Save
{
    public interface ISaveService
    {
        UniTask<SaveEnvelope> LoadLatestValidAsync(CancellationToken cancellationToken = default);
        UniTask SaveAsync(SaveEnvelope envelope, CancellationToken cancellationToken = default);
        UniTask<T> LoadPayloadAsync<T>(CancellationToken cancellationToken = default) where T : class;
        UniTask<T> LoadPayloadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
        UniTask SavePayloadAsync<T>(T payload, int version = 1, CancellationToken cancellationToken = default) where T : class;
        UniTask SavePayloadAsync<T>(string key, T payload, int version = 1, CancellationToken cancellationToken = default) where T : class;
        UniTask<string> LoadPayloadJsonAsync(string key, CancellationToken cancellationToken = default);
        UniTask SavePayloadJsonAsync(string key, string json, int version = 1, CancellationToken cancellationToken = default);
    }
}
