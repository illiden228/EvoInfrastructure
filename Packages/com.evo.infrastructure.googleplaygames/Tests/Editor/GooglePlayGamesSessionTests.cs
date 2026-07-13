using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GooglePlayGames.BasicApi;
using NUnit.Framework;

namespace Evo.Infrastructure.GooglePlayGames.Tests
{
    public sealed class GooglePlayGamesSessionTests
    {
        [Test]
        public async Task AuthenticateAsync_LeftTaskCompletes_ReturnsSuccess()
        {
            Action<SignInStatus> callback = null;
            var timeout = new UniTaskCompletionSource();
            var session = CreateSession(
                (_, sdkCallback) => callback = sdkCallback,
                (_, _) => timeout.Task);

            var authentication = session.AuthenticateAsync();
            callback?.Invoke(SignInStatus.Success);

            Assert.That(await authentication, Is.True);
            Assert.That(session.State, Is.EqualTo(GooglePlayGamesAuthenticationState.Authenticated));
        }

        [Test]
        public async Task AuthenticateAsync_TimeoutInvalidatesLateCallback()
        {
            var callbacks = new List<Action<SignInStatus>>();
            var secondTimeout = new UniTaskCompletionSource();
            var delayCall = 0;
            var warnings = 0;
            var session = CreateSession(
                (_, callback) => callbacks.Add(callback),
                (_, _) => delayCall++ == 0 ? UniTask.CompletedTask : secondTimeout.Task,
                _ => warnings++);

            Assert.That(await session.AuthenticateAsync(), Is.False);
            Assert.That(session.State, Is.EqualTo(GooglePlayGamesAuthenticationState.Unauthenticated));
            Assert.That(warnings, Is.EqualTo(1));

            var retry = session.AuthenticateAsync();
            callbacks[0].Invoke(SignInStatus.Success);
            await UniTask.Yield();
            Assert.That(retry.Status, Is.EqualTo(UniTaskStatus.Pending));

            callbacks[1].Invoke(SignInStatus.Success);
            callbacks[1].Invoke(SignInStatus.InternalError);
            Assert.That(await retry, Is.True);
            Assert.That(session.State, Is.EqualTo(GooglePlayGamesAuthenticationState.Authenticated));
        }

        [Test]
        public async Task AuthenticateAsync_CancellationDoesNotLogTimeout()
        {
            var warnings = 0;
            using var cancellation = new CancellationTokenSource();
            var session = CreateSession(
                (_, _) => { },
                (_, token) => UniTask.Delay(60000, cancellationToken: token),
                _ => warnings++);

            var authentication = session.AuthenticateAsync(cancellationToken: cancellation.Token);
            cancellation.Cancel();

            try
            {
                await authentication;
                Assert.Fail("Authentication completed successfully after cancellation.");
            }
            catch (OperationCanceledException)
            {
                // Expected.
            }

            Assert.That(warnings, Is.Zero);
            Assert.That(session.State, Is.EqualTo(GooglePlayGamesAuthenticationState.Unauthenticated));
        }

        private static GooglePlayGamesSession CreateSession(
            Action<bool, Action<SignInStatus>> authenticate,
            Func<int, CancellationToken, UniTask> delay,
            Action<string> logWarning = null)
        {
            return new GooglePlayGamesSession(
                new GooglePlayGamesOptions { authenticationTimeoutMs = 1000 },
                authenticate,
                delay,
                logWarning ?? (_ => { }));
        }
    }
}
