using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Evo.Infrastructure.Services.Identity.Tests
{
    public sealed class PlayerIdentityServiceTests
    {
        [Test]
        public void AuthenticateAsync_UsesAvailableProviderWithHighestPriority()
        {
            var lowerPriority = CreateSuccessfulProvider("lower", 10);
            var higherPriority = CreateSuccessfulProvider("higher", 20);
            var service = new PlayerIdentityService(new[] { lowerPriority, higherPriority });

            var result = Await(service.AuthenticateAsync(PlayerAuthenticationMode.Automatic));

            Assert.That(result.Identity.ProviderId, Is.EqualTo("higher"));
            Assert.That(higherPriority.AuthenticationCount, Is.EqualTo(1));
            Assert.That(lowerPriority.AuthenticationCount, Is.Zero);
        }

        [Test]
        public void AuthenticateAsync_SkipsUnavailableProviders()
        {
            var unavailable = new StubIdentityProvider(
                "unavailable",
                100,
                false,
                (_, _) => throw new AssertionException("Unavailable provider must not be invoked."));
            var available = CreateSuccessfulProvider("available", 1);
            var service = new PlayerIdentityService(new[] { unavailable, available });

            var result = Await(service.AuthenticateAsync(PlayerAuthenticationMode.Interactive));

            Assert.That(result.Identity.ProviderId, Is.EqualTo("available"));
            Assert.That(unavailable.AuthenticationCount, Is.Zero);
        }

        [Test]
        public void AuthenticateAsync_WhenEveryProviderIsUnavailable_CompletesAsUnavailable()
        {
            var unavailable = new StubIdentityProvider(
                "unavailable",
                1,
                false,
                (_, _) => throw new AssertionException("Unavailable provider must not be invoked."));
            var service = new PlayerIdentityService(new[] { unavailable });

            var result = Await(service.AuthenticateAsync(PlayerAuthenticationMode.Automatic));

            Assert.That(result.State, Is.EqualTo(PlayerAuthenticationState.Unavailable));
            Assert.That(service.State, Is.EqualTo(PlayerAuthenticationState.Unavailable));
            Assert.That(service.Current.IsValid, Is.False);
            Assert.That(unavailable.AuthenticationCount, Is.Zero);
        }

        [Test]
        public void AuthenticateAsync_WhenProviderThrows_CompletesAsFailed()
        {
            var provider = new StubIdentityProvider(
                "failing",
                1,
                true,
                (_, _) => throw new InvalidOperationException("Authentication failed."));
            var service = new PlayerIdentityService(new[] { provider });

            var result = Await(service.AuthenticateAsync(PlayerAuthenticationMode.Automatic));

            Assert.That(result.State, Is.EqualTo(PlayerAuthenticationState.Failed));
            Assert.That(result.Error, Is.EqualTo("Authentication failed."));
            Assert.That(service.State, Is.EqualTo(PlayerAuthenticationState.Failed));
            Assert.That(service.Current.IsValid, Is.False);
        }

        [Test]
        public void AuthenticateAsync_WhenCancelled_PropagatesCancellationAndLeavesUnauthenticatedState()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var provider = new StubIdentityProvider(
                "cancelled",
                1,
                true,
                (_, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return UniTask.FromResult(default(PlayerAuthenticationResult));
                });
            var service = new PlayerIdentityService(new[] { provider });

            Assert.Throws<OperationCanceledException>(() =>
                Await(service.AuthenticateAsync(PlayerAuthenticationMode.Interactive, cancellation.Token)));
            Assert.That(service.State, Is.EqualTo(PlayerAuthenticationState.Unauthenticated));
            Assert.That(service.IsAuthenticated, Is.False);
            Assert.That(service.Current.IsValid, Is.False);
        }

        [Test]
        public void AuthenticateAsync_WhenSuccessful_UpdatesCurrentIdentityAndState()
        {
            var provider = CreateSuccessfulProvider("google-play-games", 10);
            var service = new PlayerIdentityService(new[] { provider });

            var result = Await(service.AuthenticateAsync(PlayerAuthenticationMode.Automatic));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(service.IsAuthenticated, Is.True);
            Assert.That(service.State, Is.EqualTo(PlayerAuthenticationState.Authenticated));
            Assert.That(service.Current.ProviderId, Is.EqualTo("google-play-games"));
            Assert.That(service.Current.PlayerId, Is.EqualTo("player-id"));
            Assert.That(service.Current.DisplayName, Is.EqualTo("Player"));
            Assert.That(service.Current.AvatarUrl, Is.EqualTo("https://example.invalid/avatar.png"));
        }

        private static StubIdentityProvider CreateSuccessfulProvider(string providerId, int priority)
        {
            var identity = new PlayerIdentity(
                providerId,
                "player-id",
                "Player",
                "https://example.invalid/avatar.png");
            return new StubIdentityProvider(
                providerId,
                priority,
                true,
                (_, _) => UniTask.FromResult(new PlayerAuthenticationResult(
                    PlayerAuthenticationState.Authenticated,
                    identity)));
        }

        private static PlayerAuthenticationResult Await(UniTask<PlayerAuthenticationResult> task)
        {
            return task.AsTask().GetAwaiter().GetResult();
        }
    }
}
