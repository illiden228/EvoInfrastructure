using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using VContainer;

namespace Evo.Infrastructure.GooglePlayGames.Save.Tests
{
    public sealed class GooglePlayGamesSaveBackendTests
    {
        [Test]
        public void ContainerBuilder_ResolveBackend_UsesProductionConstructor()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance<IGooglePlayGamesSession>(new UnavailableGooglePlayGamesSession());
            builder.RegisterInstance(new GooglePlayGamesSaveOptions());
            builder.Register<GooglePlayGamesSaveBackend>(Lifetime.Singleton);

            using var container = builder.Build();

            Assert.That(container.Resolve<GooglePlayGamesSaveBackend>(), Is.Not.Null);
        }

        [Test]
        public async Task AwaitWithTimeoutAsync_OperationCompletes_ReturnsResult()
        {
            var timeout = new UniTaskCompletionSource();
            var backend = CreateBackend((_, _) => timeout.Task);

            var result = await backend.AwaitWithTimeoutAsync(
                UniTask.FromResult("save-data"),
                "read",
                default);

            Assert.That(result, Is.EqualTo("save-data"));
        }

        [Test]
        public async Task AwaitWithTimeoutAsync_TimeoutReturnsDefault_AndLateResultIsIsolated()
        {
            var operation = new UniTaskCompletionSource<string>();
            var retryOperation = new UniTaskCompletionSource<string>();
            var retryTimeout = new UniTaskCompletionSource();
            var delayCall = 0;
            var warnings = 0;
            var backend = CreateBackend(
                (_, _) => delayCall++ == 0 ? UniTask.CompletedTask : retryTimeout.Task,
                _ => warnings++);

            var timedOutResult = await backend.AwaitWithTimeoutAsync(operation.Task, "read", default);
            Assert.That(timedOutResult, Is.Null);
            Assert.That(warnings, Is.EqualTo(1));

            var retry = backend.AwaitWithTimeoutAsync(retryOperation.Task, "read", default);
            operation.TrySetResult("late-result");
            operation.TrySetResult("duplicate-result");
            await UniTask.Yield();
            Assert.That(retry.Status, Is.EqualTo(UniTaskStatus.Pending));

            retryOperation.TrySetResult("retry-result");
            Assert.That(await retry, Is.EqualTo("retry-result"));
        }

        private static GooglePlayGamesSaveBackend CreateBackend(
            System.Func<int, CancellationToken, UniTask> delay,
            System.Action<string> logWarning = null)
        {
            return new GooglePlayGamesSaveBackend(
                new UnavailableGooglePlayGamesSession(),
                new GooglePlayGamesSaveOptions { operationTimeoutMs = 1000 },
                delay,
                logWarning ?? (_ => { }));
        }
    }
}
